using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Medley.Application.Services;

/// <summary>
/// Service for extracting fragments from source content using AI
/// </summary>
public class FragmentExtractionService
{
    private readonly IAiProcessingService _aiService;
    private readonly IContentChunkingService _chunkingService;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IRepository<Fragment> _fragmentRepository;
    private readonly IRepository<Template> _templateRepository;
    private readonly ILogger<FragmentExtractionService> _logger;
    private readonly AiCallContext _aiCallContext;

    // Text chunking configuration
    private const int ChunkingThreshold = 90000; // Only chunk if content exceeds 90K characters

    public FragmentExtractionService(
        IAiProcessingService aiService,
        IContentChunkingService chunkingService,
        IRepository<Source> sourceRepository,
        IRepository<Fragment> fragmentRepository,
        IRepository<Template> templateRepository,
        ILogger<FragmentExtractionService> logger,
        AiCallContext aiCallContext)
    {
        _aiService = aiService;
        _chunkingService = chunkingService;
        _sourceRepository = sourceRepository;
        _fragmentRepository = fragmentRepository;
        _templateRepository = templateRepository;
        _logger = logger;
        _aiCallContext = aiCallContext;
    }

    /// <summary>
    /// Extracts fragments from a source using AI
    /// </summary>
    /// <param name="sourceId">The source ID to extract fragments from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing fragment count and extraction message</returns>
    public async Task<FragmentExtractionResult> ExtractFragmentsAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(FragmentExtractionService), nameof(ExtractFragmentsAsync), nameof(Source), sourceId))
        {
            _logger.LogInformation("Starting fragment extraction for source {SourceId}", sourceId);

            // Load source with content
            var source = await _sourceRepository.Query()
            .Include(s => s.Integration)
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source == null)
        {
            _logger.LogError("Source {SourceId} not found", sourceId);
            throw new InvalidOperationException($"Source {sourceId} not found");
        }

        if (string.IsNullOrWhiteSpace(source.Content))
        {
            _logger.LogWarning("Source {SourceId} has no content to extract fragments from", sourceId);
            throw new InvalidOperationException($"Source {sourceId} has no content");
        }

        _logger.LogInformation("Source loaded: {SourceName} (Content length: {ContentLength} characters)", 
            source.Name, source.Content.Length);

        // Check for existing fragments
        var existingFragments = await _fragmentRepository.Query()
            .Where(f => f.Source!.Id == sourceId)
            .ToListAsync(cancellationToken);

        if (existingFragments.Any())
        {
            // Check if any fragments have been clustered
            var clusteredFragments = existingFragments.Where(f => f.ClusteredIntoId != null).ToList();
            
            if (clusteredFragments.Any())
            {
                _logger.LogWarning("Cannot re-extract fragments for source {SourceId}: {Count} fragments have been clustered", 
                    sourceId, clusteredFragments.Count);
                throw new InvalidOperationException(
                    $"Cannot re-extract fragments because {clusteredFragments.Count} fragment(s) have been clustered.");
            }

            // Remove existing fragments
            _logger.LogInformation("Removing {Count} existing fragments for source {SourceId}", 
                existingFragments.Count, sourceId);
            
            foreach (var fragment in existingFragments)
            {
                await _fragmentRepository.DeleteAsync(fragment);
            }
        }

        // Retrieve the fragment extraction prompt template
        var template = await _templateRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == TemplateType.FragmentExtraction, cancellationToken);

        if (template == null)
        {
            _logger.LogError("FragmentExtraction template not found in database");
            throw new InvalidOperationException("FragmentExtraction template not configured");
        }

        // Load organization context template (if configured)
        var orgContextTemplate = await _templateRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == TemplateType.OrganizationContext, cancellationToken);

        // Build system prompt with organization context if available
        var systemPrompt = template.Content;
        if (orgContextTemplate != null && !string.IsNullOrWhiteSpace(orgContextTemplate.Content))
        {
            systemPrompt = $@"{template.Content}

## Company Context
{orgContextTemplate.Content}";
            _logger.LogInformation("Including organization context template in fragment extraction prompt");
        }

        // Try processing without chunking first (unless content is very large)
        var shouldChunk = source.Content.Length > ChunkingThreshold;
        
        if (!shouldChunk)
        {
            _logger.LogInformation("Content length ({Length}) is below chunking threshold. Processing as single request.", 
                source.Content.Length);
            
            try
            {
                var result = await ProcessSingleContentAsync(source.Content, systemPrompt, cancellationToken);
                return await SaveFragmentsAndReturnResultAsync(source, result.Fragments, result.Messages, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout occurred processing content as single request. Retrying with chunking.");
                shouldChunk = true;
            }
        }

        // Process with chunking (either because content is large or timeout occurred)
        if (shouldChunk)
        {
            _logger.LogInformation("Processing content with chunking (length: {Length} characters)", source.Content.Length);
            var result = await ProcessChunkedContentAsync(source, systemPrompt, cancellationToken);
            return await SaveFragmentsAndReturnResultAsync(source, result.Fragments, result.Messages, cancellationToken);
        }

        throw new InvalidOperationException("Fragment extraction failed unexpectedly");
    }
}

    private async Task<(List<FragmentDto> Fragments, List<string> Messages)> ProcessSingleContentAsync(
        string content, 
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        // Set nested context for this operation (parent context already set in ExtractFragmentsAsync)
        using (_aiCallContext.SetContext(nameof(FragmentExtractionService), nameof(ProcessSingleContentAsync)))
        {
            var extractionResponse = await _aiService.ProcessStructuredPromptAsync<FragmentExtractionResponse>(
                userPrompt: content,
                systemPrompt: systemPrompt,
                cancellationToken: cancellationToken);

            var fragments = new List<FragmentDto>();
            var messages = new List<string>();

            if (extractionResponse != null)
            {
                if (!string.IsNullOrWhiteSpace(extractionResponse.Message))
                {
                    messages.Add(extractionResponse.Message.Trim());
                }

                if (extractionResponse.Fragments != null && extractionResponse.Fragments.Count > 0)
                {
                    fragments.AddRange(extractionResponse.Fragments);
                    _logger.LogInformation("Extracted {Count} fragments from content", extractionResponse.Fragments.Count);
                }
            }

            return (fragments, messages);
        }
    }

    private async Task<(List<FragmentDto> Fragments, List<string> Messages)> ProcessChunkedContentAsync(
        Source source, 
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        // Use the chunking service to intelligently chunk the content
        var contentChunks = await _chunkingService.ChunkContentAsync(source, cancellationToken);

        if (contentChunks.Count == 0)
        {
            _logger.LogWarning("Chunking service returned no chunks for source {SourceId}", source.Id);
            return (new List<FragmentDto>(), new List<string>());
        }

        // Process all chunks in parallel
        var chunkTasks = contentChunks.Select(async chunk =>
        {
            var chunkNumber = chunk.Index + 1;
            _logger.LogInformation("Processing chunk {ChunkNumber} of {TotalChunks} (length: {ChunkLength} characters, topic: {Topic})",
                chunkNumber, contentChunks.Count, chunk.Content.Length, chunk.Summary ?? "N/A");

            try
            {
                // Track this specific chunk operation
                using (_aiCallContext.SetContext(nameof(ContentChunkingService), $"Chunk{chunkNumber}", nameof(Source), source.Id))
                {
                    var extractionResponse = await _aiService.ProcessStructuredPromptAsync<FragmentExtractionResponse>(
                        userPrompt: chunk.Content,
                        systemPrompt: systemPrompt,
                        cancellationToken: cancellationToken);

                    if (extractionResponse != null)
                    {
                        var message = !string.IsNullOrWhiteSpace(extractionResponse.Message) 
                            ? extractionResponse.Message.Trim() 
                            : null;

                        var fragments = extractionResponse.Fragments ?? new List<FragmentDto>();
                        
                        _logger.LogInformation("Extracted {Count} fragments from chunk {ChunkNumber}",
                            fragments.Count, chunkNumber);

                        return (Fragments: fragments, Message: message, ChunkNumber: chunkNumber);
                    }

                    return (Fragments: new List<FragmentDto>(), Message: (string?)null, ChunkNumber: chunkNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process chunk {ChunkNumber} of {TotalChunks}",
                    chunkNumber, contentChunks.Count);
                throw new InvalidOperationException(
                    $"Fragment extraction failed while processing chunk {chunkNumber} of {contentChunks.Count}: {ex.Message}", ex);
            }
        }).ToList();

        var chunkResults = await Task.WhenAll(chunkTasks);

        // Aggregate results from all chunks
        var allFragments = chunkResults.SelectMany(r => r.Fragments).ToList();
        var allMessages = chunkResults.Where(r => r.Message != null).Select(r => r.Message!).ToList();

        _logger.LogInformation("Aggregated {TotalCount} fragments from {ChunkCount} chunks",
            allFragments.Count, contentChunks.Count);

        return (allFragments, allMessages);
    }

    private async Task<FragmentExtractionResult> SaveFragmentsAndReturnResultAsync(
        Source source,
        List<FragmentDto> fragmentDtos,
        List<string> messages,
        CancellationToken cancellationToken = default)
    {

        // Combine all messages into a single message
        var combinedMessage = messages.Count > 0 
            ? string.Join("\n\n", messages) 
            : null;

        // Zero fragments is now a successful outcome, not an error
        if (fragmentDtos.Count == 0)
        {
            _logger.LogInformation("No fragments were extracted for source {SourceId}. This is a successful outcome.", source.Id);
            if (string.IsNullOrWhiteSpace(combinedMessage))
            {
                combinedMessage = "No fragments were extracted from the content.";
            }
        }

        // Create Fragment entities and save them
        var fragmentCount = 0;
        foreach (var fragmentDto in fragmentDtos)
        {
            try
            {
                var fragment = new Fragment
                {
                    Title = fragmentDto.Title.Trim().Substring(0, Math.Min(200, fragmentDto.Title.Trim().Length)),
                    Summary = fragmentDto.Summary.Trim().Substring(0, Math.Min(500, fragmentDto.Summary.Trim().Length)),
                    Category = fragmentDto.Category.Trim().Substring(0, Math.Min(100, fragmentDto.Category.Trim().Length)),
                    Content = fragmentDto.Content.Trim().Substring(0, Math.Min(10000, fragmentDto.Content.Trim().Length)),
                    Source = source,
                    LastModifiedAt = DateTimeOffset.UtcNow,
                };

                await _fragmentRepository.AddAsync(fragment);
                fragmentCount++;

                _logger.LogDebug("Created fragment: {Title}", fragment.Title);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create fragment from DTO: {Title}", fragmentDto.Title);
                // Continue with other fragments
            }
        }

        _logger.LogInformation("Successfully extracted {Count} fragments from source {SourceId}", 
            fragmentCount, source.Id);

        // Save the extraction message to the source
        if (!string.IsNullOrWhiteSpace(combinedMessage))
        {
            source.ExtractionMessage = combinedMessage.Length > 2000 
                ? combinedMessage.Substring(0, 2000) 
                : combinedMessage;
            // Entity is already tracked, changes will be saved on SaveChangesAsync
            _logger.LogDebug("Saved extraction message to source {SourceId}", source.Id);
        }

        return new FragmentExtractionResult
        {
            FragmentCount = fragmentCount,
            Message = combinedMessage
        };
    }
}

/// <summary>
/// DTO for AI response containing extracted fragments
/// </summary>
public class FragmentExtractionResponse
{
    public List<FragmentDto> Fragments { get; set; } = new();
    [Description("Any comments or information besides fragments goes here.")]
    public string? Message { get; set; }
}

/// <summary>
/// DTO for individual fragment from AI response
/// </summary>
public class FragmentDto
{
    [Required]
    [Description("Clear, descriptive heading")]
    public required string Title { get; set; }
    
    [Required]
    [Description("Short, human-readable condensation of the full content")]
    public required string Summary { get; set; }
    
    [Required]
    [Description("Which of the supplied categories this content applies to")]
    public required string Category { get; set; }
    
    [Required]
    [Description("Markdown-formatted content")]
    public required string Content { get; set; }
}

/// <summary>
/// Result of fragment extraction operation
/// </summary>
public class FragmentExtractionResult
{
    public int FragmentCount { get; set; }
    public string? Message { get; set; }
}

