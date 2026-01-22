using Hangfire;
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
        _logger.LogInformation(logMessage);

        try
        {
            // Check organization settings (no transaction needed for read-only check)
            var organization = await _organizationRepository.Query().FirstOrDefaultAsync(cancellationToken);
            if (organization == null)
            {
                throw new InvalidOperationException("No organization found; speaker extraction cannot proceed.");
            }

            if (!organization.EnableSpeakerExtraction)
            {
                _logger.LogInformation("Speaker extraction is disabled for organization {OrganizationId}. Skipping SpeakerExtraction job.",
                    organization.Id);
                return;
            }

            // Get the batch of sources to process
            var query = _sourceRepository.Query()
                .Where(s => s.SpeakersExtracted == null && s.MetadataType == SourceMetadataType.Collector_Fellow);

            // Filter by source if specified
            if (sourceId.HasValue)
            {
                query = query.Where(s => s.Id == sourceId.Value);
            }

            var sourceIds = await query
                .Select(s => s.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (sourceIds.Count == 0)
            {
                _logger.LogInformation("No sources pending speaker extraction.");
                return;
            }

            _logger.LogInformation("Processing batch of {Count} sources", sourceIds.Count);

            int processedCount = 0;
            int speakersExtractedCount = 0;
            int errorCount = 0;

            // Process each source in its own transaction
            foreach (var currentSourceId in sourceIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancellation requested. Stopping batch processing.");
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
                    _logger.LogError(ex, "Error processing source {SourceId}", currentSourceId);
                    
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
                        _logger.LogError(markEx, "Failed to mark source {SourceId} as processed after error", currentSourceId);
                    }
                }
            }

            _logger.LogInformation(
                "SpeakerExtraction batch completed. Processed: {Processed}/{Total}, Speakers extracted: {Speakers}, Errors: {Errors}",
                processedCount, sourceIds.Count, speakersExtractedCount, errorCount);

            if (sourceIds.Count == BatchSize && !sourceId.HasValue)
            {
                _logger.LogInformation("Rescheduling SpeakerExtraction job for remaining sources");
                
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
                _logger.LogInformation(completionMessage);
            }
            
            _logger.LogInformation("SpeakerExtraction job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SpeakerExtraction job failed");
            throw;
        }
    }

    /// <summary>
    /// Extracts speakers from a single source
    /// </summary>
    /// <returns>Number of speakers extracted</returns>
    private async Task<int> ExtractSpeakersFromSourceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.Query()
            .Include(s => s.Speakers)
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source == null)
        {
            _logger.LogWarning("Source {SourceId} not found", sourceId);
            return 0;
        }

        // Get organization for email domain matching
        var organization = await _organizationRepository.Query().FirstOrDefaultAsync(cancellationToken);
        var organizationEmailDomain = organization?.EmailDomain?.ToLowerInvariant();

        try
        {
            // Deserialize metadata
            FellowRecordingImportModel? recording;
            try
            {
                recording = JsonSerializer.Deserialize<FellowRecordingImportModel>(source.MetadataJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize metadata for source {SourceId}. Marking as processed.", sourceId);
                source.SpeakersExtracted = DateTimeOffset.UtcNow;
                return 0;
            }

            if (recording?.Transcript?.SpeechSegments == null || recording.Transcript.SpeechSegments.Count == 0)
            {
                _logger.LogDebug("No speech segments found for source {SourceId}", sourceId);
                source.SpeakersExtracted = DateTimeOffset.UtcNow;
                return 0;
            }

            // Extract unique speaker names
            var speakers = recording.Transcript.SpeechSegments
                .Where(s => !string.IsNullOrWhiteSpace(s.Speaker))
                .Select(s => new { Speaker = RemoveSpeakerSuffixes(s.Speaker!), TextLength = s.Text?.Length ?? 0 })
                .Where(s => !IsPhoneNumber(s.Speaker))
                .GroupBy(x => x.Speaker)
                .Select(g => new { Speaker = g.Key, TotalLength = g.Sum(x => x.TextLength) })
                .ToList();

            if (speakers.Count == 0)
            {
                _logger.LogDebug("No valid speakers found for source {SourceId}", sourceId);
                source.SpeakersExtracted = DateTimeOffset.UtcNow;
                return 0;
            }

            var speakerNames = speakers.Select(x => x.Speaker).ToList();

            _logger.LogDebug("Found {Count} unique speakers for source {SourceId}: {Speakers}",
                speakerNames.Count, sourceId, string.Join(", ", speakerNames));

            // Get attendee emails for IsInternal matching
            var attendeeEmails = recording.Note?.EventAttendees?
                .Where(a => !string.IsNullOrWhiteSpace(a.Email))
                .Select(a => a.Email!.ToLowerInvariant())
                .ToList() ?? new List<string>();

            // Find or create speakers
            var existingSpeakers = await _speakerRepository.Query()
                .Where(sp => speakerNames.Contains(sp.Name))
                .ToListAsync(cancellationToken);

            var existingSpeakerNames = existingSpeakers.Select(sp => sp.Name).ToHashSet();
            var newSpeakerNames = speakerNames.Where(name => !existingSpeakerNames.Contains(name)).ToList();

            // Create new speakers
            foreach (var speakerName in newSpeakerNames)
            {
                var (isInternal, email) = DetermineIsInternalAndEmail(speakerName, attendeeEmails, organizationEmailDomain);
                
                var speaker = new Speaker
                {
                    Name = speakerName,
                    Email = email,
                    IsInternal = isInternal,
                    TrustLevel = null
                };
                await _speakerRepository.AddAsync(speaker);
                existingSpeakers.Add(speaker);
                
                _logger.LogDebug("Created new speaker: {SpeakerName} with IsInternal={IsInternal}, Email={Email}",
                    speakerName, isInternal?.ToString() ?? "null", email ?? "null");
            }

            // Associate speakers with source
            source.Speakers.Clear();
            foreach (var speaker in existingSpeakers)
            {
                source.Speakers.Add(speaker);
            }


            if (source.PrimarySpeakerId == null)
            {
                // Determine primary speaker by total transcript length (only internal speakers)
                var speakerLengths = speakers.ToDictionary(x => x.Speaker, x => x.TotalLength);

                source.PrimarySpeakerId = existingSpeakers
                    .OrderByDescending(x => x.IsInternal == true)
                    .ThenByDescending(x => speakerLengths.ContainsKey(x.Name) ? speakerLengths[x.Name] : 0)
                    .FirstOrDefault()?.Id;
            }

            source.SpeakersExtracted = DateTimeOffset.UtcNow;

            _logger.LogInformation("Extracted {Count} speakers for source {SourceId} ({SourceName})",
                existingSpeakers.Count, sourceId, source.Name);

            return existingSpeakers.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting speakers from source {SourceId}", sourceId);
            throw;
        }
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
