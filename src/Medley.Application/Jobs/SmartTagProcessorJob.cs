using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for automatically tagging sources as internal/external based on attendee domains and meeting name patterns
/// </summary>
public class SmartTagProcessorJob : BaseHangfireJob<SmartTagProcessorJob>
{
    private readonly IRepository<Source> _sourceRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly ISourceMetadataProvider _metadataProvider;

    public SmartTagProcessorJob(
        IRepository<Source> sourceRepository,
        IRepository<Organization> organizationRepository,
        ISourceMetadataProvider metadataProvider,
        IUnitOfWork unitOfWork,
        ILogger<SmartTagProcessorJob> logger) : base(unitOfWork, logger)
    {
        _sourceRepository = sourceRepository;
        _organizationRepository = organizationRepository;
        _metadataProvider = metadataProvider;
    }

    /// <summary>
    /// Executes the smart tag processing job
    /// </summary>
    public async Task ExecuteAsync()
    {
        await ExecuteWithTransactionAsync(async () =>
        {
            _logger.LogInformation("Starting SmartTagProcessor job");

            // Get organization's email domain (assuming single-tenant for now)
            var organization = await _organizationRepository.Query()
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                _logger.LogWarning("No organization found. Skipping SmartTagProcessor job.");
                return;
            }

            if (!organization.EnableSmartTagging)
            {
                _logger.LogInformation("Smart tagging is disabled for organization {OrganizationId}. Skipping SmartTagProcessor job.",
                    organization.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(organization.EmailDomain))
            {
                _logger.LogWarning("Organization {OrganizationId} has no EmailDomain configured. Skipping SmartTagProcessor job.",
                    organization.Id);
                return;
            }

            var emailDomain = organization.EmailDomain.Trim();
            _logger.LogInformation("Using email domain: {EmailDomain}", emailDomain);

            // Query sources where IsInternal is null
            var sourcesToProcess = await _sourceRepository.Query()
                .Where(s => s.IsInternal == null)
                .ToListAsync();

            if (sourcesToProcess.Count == 0)
            {
                _logger.LogInformation("No sources with null IsInternal found. Job completed.");
                return;
            }

            _logger.LogInformation("Found {Count} sources to process", sourcesToProcess.Count);

            var processedCount = 0;
            var taggedInternalCount = 0;
            const int batchSize = 100;

            // Process sources in batches
            for (int i = 0; i < sourcesToProcess.Count; i += batchSize)
            {
                var batch = sourcesToProcess.Skip(i).Take(batchSize).ToList();
                
                foreach (var source in batch)
                {
                    var isInternal = DetermineIsInternal(source, emailDomain);
                    
                    if (isInternal.HasValue)
                    {
                        source.IsInternal = isInternal.Value;
                        await _sourceRepository.SaveAsync(source);
                        
                        if (isInternal.Value)
                            taggedInternalCount++;
                        
                        processedCount++;
                        
                        _logger.LogDebug("Tagged source {SourceId} ({Name}) as IsInternal={IsInternal}",
                            source.Id, source.Name, isInternal.Value);
                    }
                }

                // Save changes for this batch
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogDebug("Processed batch {BatchNumber}. Total processed: {Processed}, Tagged internal: {TaggedInternal}",
                    (i / batchSize) + 1, processedCount, taggedInternalCount);
            }

            _logger.LogInformation("SmartTagProcessor job completed. Processed: {Processed}, Tagged as internal: {TaggedInternal}",
                processedCount, taggedInternalCount);
        });
    }

    /// <summary>
    /// Determines if a source should be marked as internal based on attendee emails and name patterns
    /// </summary>
    /// <param name="source">The source to evaluate</param>
    /// <param name="emailDomain">The organization's email domain</param>
    /// <returns>true if internal, false if external, null if cannot be determined</returns>
    private bool? DetermineIsInternal(Source source, string emailDomain)
    {
        // Normalize email domain for comparison (remove leading @ if present)
        var normalizedDomain = emailDomain.StartsWith("@", StringComparison.OrdinalIgnoreCase)
            ? emailDomain.Substring(1)
            : emailDomain;

        // Get attendee emails from the source
        var attendeeEmails = _metadataProvider.GetAttendeeEmails(source);

        if (attendeeEmails.Count > 0)
        {
            // Check if ALL attendees are from the organization's domain
            var allInternal = attendeeEmails.All(email =>
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                // Extract domain from email (everything after @)
                var emailParts = email.Split('@');
                if (emailParts.Length != 2)
                    return false;

                var attendeeDomain = emailParts[1].Trim();
                return string.Equals(attendeeDomain, normalizedDomain, StringComparison.OrdinalIgnoreCase);
            });

            return allInternal;
        }

        // If no attendees found, check if source name contains "INTERNAL" (case-insensitive)
        if (!string.IsNullOrWhiteSpace(source.Name))
        {
            if (source.Name.Contains("INTERNAL", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Cannot determine - leave as null
        return null;
    }
}
