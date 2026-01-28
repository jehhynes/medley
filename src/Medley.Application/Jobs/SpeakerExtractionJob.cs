using Hangfire;
using Hangfire.Console;
using Hangfire.MissionControl;
using Hangfire.Server;
using Medley.Application.Integrations.Models.Collector;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for extracting speakers from meeting transcripts.
/// Processes sources in batches of 10, with each source in its own transaction.
/// </summary>
[MissionLauncher]
public class SpeakerExtractionJob : BaseHangfireJob<SpeakerExtractionJob>
{
    private const int BatchSize = 100;
    
    // Regex to detect phone numbers - matches any string containing only numbers, +, space, -, and *
    // This catches masked numbers like "+1 309-***-**57" or "+1 516-***-**78"
    private static readonly Regex PhoneNumberRegex = new Regex(
        @"^[\+\d\s\-\*]+$",
        RegexOptions.Compiled);
    
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IRepository<Speaker> _speakerRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SpeakerExtractionJob(
        IRepository<Organization> organizationRepository,
        IRepository<Source> sourceRepository,
        IRepository<Speaker> speakerRepository,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<SpeakerExtractionJob> logger) : base(unitOfWork, logger)
    {
        _organizationRepository = organizationRepository;
        _sourceRepository = sourceRepository;
        _speakerRepository = speakerRepository;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Executes the speaker extraction job.
    /// Processes up to 100 sources and reschedules itself if more remain.
    /// </summary>
    /// <param name="sourceId">Optional source ID to process a specific source only</param>
    [Mission]
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken)
    {
        await ExecuteAsync(context, cancellationToken, null);
    }


    /// <summary>
    /// Executes the speaker extraction job.
    /// Processes up to 100 sources and reschedules itself if more remain.
    /// </summary>
    /// <param name="sourceId">Optional source ID to process a specific source only</param>
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken, Guid? sourceId = null)
    {
        var logMessage = sourceId.HasValue
            ? $"Starting SpeakerExtraction job for source {sourceId.Value}"
            : "Starting SpeakerExtraction job";
        LogInfo(context, logMessage);

        try
        {
            // Check organization settings (no transaction needed for read-only check)
            var organization = await _organizationRepository.Query().SingleAsync(cancellationToken);

            if (!organization.EnableSpeakerExtraction)
            {
                LogInfo(context, $"Speaker extraction is disabled for organization {organization.Id}. Skipping SpeakerExtraction job.");
                return;
            }

            // Get the batch of sources to process (both Fellow and Google Drive)
            var query = _sourceRepository.Query()
                .Where(s => s.SpeakersExtracted == null && 
                       (s.MetadataType == SourceMetadataType.Collector_Fellow || 
                        s.MetadataType == SourceMetadataType.Collector_GoogleDrive));

            // Filter by source if specified
            if (sourceId.HasValue)
            {
                query = query.Where(s => s.Id == sourceId.Value);
            }

            var sourceIds = await query
                .OrderByDescending(s => s.Date)
                .Select(s => s.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (sourceIds.Count == 0)
            {
                LogInfo(context, "No sources pending speaker extraction.");
                return;
            }

            LogInfo(context, $"Processing batch of {sourceIds.Count} sources");

            int processedCount = 0;
            int speakersExtractedCount = 0;
            int errorCount = 0;

            // Process each source in its own transaction
            foreach (var currentSourceId in sourceIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogInfo(context, "Cancellation requested. Stopping batch processing.");
                    break;
                }

                try
                {
                    await ExecuteWithTransactionAsync(async () =>
                    {
                        var extractedCount = await ExtractSpeakersFromSourceAsync(currentSourceId, cancellationToken);
                        
                        processedCount++;
                        speakersExtractedCount += extractedCount;
                    });
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LogError(context, ex, $"Error processing source {currentSourceId}");
                    
                    // Mark as processed even on error to avoid retry loops
                    try
                    {
                        await ExecuteWithTransactionAsync(async () =>
                        {
                            var source = await _sourceRepository.GetByIdAsync(currentSourceId, cancellationToken);
                            if (source != null)
                            {
                                source.SpeakersExtracted = DateTimeOffset.UtcNow;
                            }
                        });
                    }
                    catch (Exception markEx)
                    {
                        LogError(context, markEx, $"Failed to mark source {currentSourceId} as processed after error");
                    }
                }
            }

            LogInfo(context, $"Batch completed: {processedCount}/{sourceIds.Count} processed, {speakersExtractedCount} speakers extracted, {errorCount} errors");

            if (sourceIds.Count == BatchSize && !sourceId.HasValue)
            {
                LogInfo(context, "Rescheduling SpeakerExtraction job for remaining sources");
                
                // Continue with the next batch after this job completes
                var currentJobId = context.BackgroundJob.Id;
                _backgroundJobClient.ContinueJobWith<SpeakerExtractionJob>(
                    currentJobId,
                    job => job.ExecuteAsync(default!, default, null));
            }
            else
            {
                var completionMessage = sourceId.HasValue
                    ? $"Source {sourceId.Value} has been processed"
                    : "All sources have been processed";
                LogInfo(context, completionMessage);
            }
            
            LogInfo(context, "SpeakerExtraction job completed successfully");
        }
        catch (Exception ex)
        {
            LogError(context, ex, "SpeakerExtraction job failed");
            throw;
        }
    }

    /// <summary>
    /// Extracts speakers from a single source based on its metadata type
    /// </summary>
    /// <returns>Number of speakers extracted</returns>
    private async Task<int> ExtractSpeakersFromSourceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.Query()
            .Include(s => s.Speakers)
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source == null)
        {
            LogDebug($"Source {sourceId} not found");
            return 0;
        }

        // Get organization for email domain matching
        var organization = await _organizationRepository.Query().SingleAsync(cancellationToken);

        try
        {
            // Route to appropriate handler based on metadata type
            return source.MetadataType switch
            {
                SourceMetadataType.Collector_Fellow => await ExtractSpeakersFromFellowSourceAsync(source, organization, cancellationToken),
                SourceMetadataType.Collector_GoogleDrive => await ExtractSpeakersFromGoogleDriveSourceAsync(source, organization, cancellationToken),
                _ => await HandleUnsupportedMetadataTypeAsync(source, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            LogDebug($"Error extracting speakers from source {sourceId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Extracts speakers from a Fellow.ai recording source
    /// </summary>
    private async Task<int> ExtractSpeakersFromFellowSourceAsync(Source source, Organization? organization, CancellationToken cancellationToken)
    {
        // Deserialize metadata
        FellowRecordingImportModel? recording;
        try
        {
            recording = JsonSerializer.Deserialize<FellowRecordingImportModel>(source.MetadataJson);
        }
        catch (JsonException)
        {
            LogDebug($"Failed to deserialize Fellow metadata for source {source.Id}. Marking as processed.");
            source.SpeakersExtracted = DateTimeOffset.UtcNow;
            return 0;
        }

        if (recording?.Transcript?.SpeechSegments == null || recording.Transcript.SpeechSegments.Count == 0)
        {
            LogDebug($"No speech segments found for Fellow source {source.Id}");
            source.SpeakersExtracted = DateTimeOffset.UtcNow;
            return 0;
        }

        // Extract unique speaker names with text lengths
        var speakerData = recording.Transcript.SpeechSegments
            .Where(s => !string.IsNullOrWhiteSpace(s.Speaker))
            .Select(s => new { Speaker = RemoveSpeakerSuffixes(s.Speaker!), TextLength = s.Text?.Length ?? 0 })
            .Where(s => !IsPhoneNumber(s.Speaker))
            .GroupBy(x => x.Speaker)
            .Select(g => new { Speaker = g.Key, TotalLength = g.Sum(x => x.TextLength) })
            .ToList();

        if (speakerData.Count == 0)
        {
            LogDebug($"No valid speakers found for Fellow source {source.Id}");
            source.SpeakersExtracted = DateTimeOffset.UtcNow;
            return 0;
        }

        var speakerNames = speakerData.Select(x => x.Speaker).ToList();

        LogDebug($"Found {speakerNames.Count} unique speakers for Fellow source {source.Id}: {string.Join(", ", speakerNames)}");

        // Get attendee emails for IsInternal matching
        var attendeeEmails = recording.Note?.EventAttendees?
            .Where(a => !string.IsNullOrWhiteSpace(a.Email))
            .Select(a => a.Email!.ToLowerInvariant())
            .ToList() ?? new List<string>();

        // Create speaker info list
        var speakerInfoList = speakerNames.Select(name => new SpeakerInfo
        {
            Name = name,
            Email = null,
            IsInternal = null
        }).ToList();

        // Determine IsInternal and Email for each speaker
        var organizationEmailDomain = organization?.EmailDomain?.ToLowerInvariant();
        foreach (var speakerInfo in speakerInfoList)
        {
            var (isInternal, email) = DetermineIsInternalAndEmail(speakerInfo.Name, attendeeEmails, organizationEmailDomain);
            speakerInfo.IsInternal = isInternal;
            speakerInfo.Email = email;
        }

        // Find or create speakers
        var speakers = await FindOrCreateSpeakersAsync(speakerInfoList, cancellationToken);

        // Determine primary speaker by total transcript length (prefer internal speakers)
        var speakerLengths = speakerData.ToDictionary(x => x.Speaker, x => x.TotalLength);
        var primarySpeaker = speakers
            .OrderByDescending(x => x.IsInternal == true)
            .ThenByDescending(x => speakerLengths.ContainsKey(x.Name) ? speakerLengths[x.Name] : 0)
            .FirstOrDefault();

        // Associate speakers with source
        await AssociateSpeakersWithSourceAsync(source, speakers, primarySpeaker?.Id, cancellationToken);

        source.SpeakersExtracted = DateTimeOffset.UtcNow;

        LogDebug($"Extracted {speakers.Count} speakers for Fellow source {source.Id} ({source.Name})");

        return speakers.Count;
    }

    /// <summary>
    /// Extracts speakers from a Google Drive video source
    /// </summary>
    private async Task<int> ExtractSpeakersFromGoogleDriveSourceAsync(Source source, Organization? organization, CancellationToken cancellationToken)
    {
        // Deserialize metadata
        GoogleDriveVideoImportModel? video;
        try
        {
            video = JsonSerializer.Deserialize<GoogleDriveVideoImportModel>(source.MetadataJson);
        }
        catch (JsonException)
        {
            LogDebug($"Failed to deserialize Google Drive metadata for source {source.Id}. Marking as processed.");
            source.SpeakersExtracted = DateTimeOffset.UtcNow;
            return 0;
        }

        if (video == null)
        {
            LogDebug($"No video metadata found for Google Drive source {source.Id}");
            source.SpeakersExtracted = DateTimeOffset.UtcNow;
            return 0;
        }

        // Google Drive videos have a single speaker - the last modifying user
        if (string.IsNullOrWhiteSpace(video.LastModifyingUserDisplayName))
        {
            LogDebug($"No LastModifyingUserDisplayName found for Google Drive source {source.Id}");
            source.SpeakersExtracted = DateTimeOffset.UtcNow;
            return 0;
        }

        var speakerName = video.LastModifyingUserDisplayName.Trim();
        var speakerEmail = string.IsNullOrWhiteSpace(video.LastModifyingUserEmail) 
            ? null 
            : video.LastModifyingUserEmail.ToLowerInvariant();

        Speaker? existingSpeaker = null;

        // If email is missing, try to infer it from existing speakers
        if (string.IsNullOrWhiteSpace(speakerEmail) && !string.IsNullOrWhiteSpace(organization?.EmailDomain))
        {
            var inferredEmail = $"{speakerName.ToLowerInvariant()}@{organization.EmailDomain.ToLowerInvariant()}";
            
            // Check if a speaker with this inferred email exists
            existingSpeaker = await _speakerRepository.Query()
                .FirstOrDefaultAsync(s => s.Email == inferredEmail, cancellationToken);
            
            if (existingSpeaker != null)
            {
                speakerEmail = inferredEmail;
                LogDebug($"Inferred email {speakerEmail} for Google Drive source {source.Id} from existing speaker {existingSpeaker.Id}");
            }
        }

        LogDebug($"Found speaker for Google Drive source {source.Id}: {speakerName} ({speakerEmail ?? "no email"})");

        List<Speaker> speakers;

        // If we found an existing speaker through inference, use it directly
        if (existingSpeaker != null)
        {
            speakers = new List<Speaker> { existingSpeaker };
        }
        else
        {
            // All Google Drive speakers are marked as internal
            var isInternal = true;

            // Create speaker info
            var speakerInfoList = new List<SpeakerInfo>
            {
                new SpeakerInfo
                {
                    Name = speakerName,
                    Email = speakerEmail,
                    IsInternal = isInternal
                }
            };

            // Find or create speaker
            speakers = await FindOrCreateSpeakersAsync(speakerInfoList, cancellationToken);
        }

        // For Google Drive, the single speaker is always the primary speaker
        var primarySpeaker = speakers.FirstOrDefault();

        // Associate speaker with source
        await AssociateSpeakersWithSourceAsync(source, speakers, primarySpeaker?.Id, cancellationToken);

        source.SpeakersExtracted = DateTimeOffset.UtcNow;

        LogDebug($"Extracted {speakers.Count} speaker(s) for Google Drive source {source.Id} ({source.Name})");

        return speakers.Count;
    }

    /// <summary>
    /// Handles sources with unsupported metadata types
    /// </summary>
    private async Task<int> HandleUnsupportedMetadataTypeAsync(Source source, CancellationToken cancellationToken)
    {
        LogDebug($"Unsupported metadata type {source.MetadataType} for source {source.Id}. Marking as processed.");
        source.SpeakersExtracted = DateTimeOffset.UtcNow;
        return 0;
    }

    /// <summary>
    /// Finds existing speakers or creates new ones based on the provided speaker information
    /// </summary>
    private async Task<List<Speaker>> FindOrCreateSpeakersAsync(List<SpeakerInfo> speakerInfoList, CancellationToken cancellationToken)
    {
        var speakerNames = speakerInfoList.Select(s => s.Name).ToList();

        // Find existing speakers
        var existingSpeakers = await _speakerRepository.Query()
            .Where(sp => speakerNames.Contains(sp.Name))
            .ToListAsync(cancellationToken);

        var existingSpeakerNames = existingSpeakers.Select(sp => sp.Name).ToHashSet();
        var newSpeakerInfos = speakerInfoList.Where(info => !existingSpeakerNames.Contains(info.Name)).ToList();

        // Create new speakers
        foreach (var speakerInfo in newSpeakerInfos)
        {
            var speaker = new Speaker
            {
                Name = speakerInfo.Name,
                Email = speakerInfo.Email,
                IsInternal = speakerInfo.IsInternal,
                TrustLevel = null
            };
            await _speakerRepository.AddAsync(speaker);
            existingSpeakers.Add(speaker);

            LogDebug($"Created new speaker: {speakerInfo.Name} with IsInternal={speakerInfo.IsInternal?.ToString() ?? "null"}, Email={speakerInfo.Email ?? "null"}");
        }

        return existingSpeakers;
    }

    /// <summary>
    /// Associates speakers with a source and sets the primary speaker
    /// </summary>
    private async Task AssociateSpeakersWithSourceAsync(Source source, List<Speaker> speakers, Guid? primarySpeakerId, CancellationToken cancellationToken)
    {
        // Clear existing associations
        source.Speakers.Clear();

        // Add new associations
        foreach (var speaker in speakers)
        {
            source.Speakers.Add(speaker);
        }

        // Set primary speaker if not already set
        if (source.PrimarySpeakerId == null && primarySpeakerId.HasValue)
        {
            source.PrimarySpeakerId = primarySpeakerId;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Helper class to hold speaker information during processing
    /// </summary>
    private class SpeakerInfo
    {
        public required string Name { get; set; }
        public string? Email { get; set; }
        public bool? IsInternal { get; set; }
    }

    /// <summary>
    /// Determines if a string is a phone number
    /// </summary>
    private static bool IsPhoneNumber(string value)
    {
        return PhoneNumberRegex.IsMatch(value);
    }

    /// <summary>
    /// Removes common suffixes from speaker names like (1), (2), - A, - B, (A), (B)
    /// Continues removing suffixes until no more are found (handles multiple suffixes like "Name (1) - A")
    /// </summary>
    private static string RemoveSpeakerSuffixes(string speakerName)
    {
        // First trim any whitespace
        speakerName = speakerName.Trim();
        
        bool changed;
        do
        {
            changed = false;
            var original = speakerName;
            
            // Remove suffixes like (1), (2), (3), etc.
            speakerName = Regex.Replace(speakerName, @"\s*\(\d+\)$", "").Trim();
            if (speakerName != original)
            {
                changed = true;
                continue;
            }
            
            // Remove suffixes like (A), (B), (C), etc.
            speakerName = Regex.Replace(speakerName, @"\s*\([A-Z]\)$", "").Trim();
            if (speakerName != original)
            {
                changed = true;
                continue;
            }
            
            // Remove suffixes like - A, - B, - C, etc.
            speakerName = Regex.Replace(speakerName, @"\s*-\s*[A-Z]$", "").Trim();
            if (speakerName != original)
            {
                changed = true;
            }
        } while (changed);
        
        return speakerName;
    }

    /// <summary>
    /// Determines if a speaker is internal by matching their name with attendee emails
    /// </summary>
    /// <param name="speakerName">Full name of the speaker</param>
    /// <param name="attendeeEmails">List of attendee email addresses</param>
    /// <param name="organizationEmailDomain">Organization's email domain (e.g., "company.com")</param>
    /// <returns>Tuple of (IsInternal, Email) - IsInternal: True if internal, null if cannot determine; Email: matched email or null</returns>
    private static (bool? IsInternal, string? Email) DetermineIsInternalAndEmail(string speakerName, List<string> attendeeEmails, string? organizationEmailDomain)
    {
        if (string.IsNullOrWhiteSpace(organizationEmailDomain) || attendeeEmails.Count == 0)
        {
            return (null, null);
        }

        // Extract first name and last name
        var nameParts = speakerName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length < 2)
        {
            return (null, null); // Cannot determine without both first and last name
        }

        var firstName = nameParts[0].ToLowerInvariant();
        var lastName = nameParts[1].ToLowerInvariant();
        var lastInitial = lastName[0];

        // Build expected email format: firstnamelastinitial@domain.com
        var expectedLocalPart = $"{firstName}{lastInitial}";

        // Try to find a matching email with the organization domain
        foreach (var email in attendeeEmails)
        {
            // Check if email is from the organization domain
            if (!email.EndsWith($"@{organizationEmailDomain}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Extract the local part of the email (before @)
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
            {
                continue;
            }

            var localPart = email.Substring(0, atIndex).ToLowerInvariant();

            // Check if the local part matches the expected format
            if (localPart == expectedLocalPart)
            {
                return (true, email); // Found a match with organization domain
            }
        }

        return (null, null); // No match found
    }
}
