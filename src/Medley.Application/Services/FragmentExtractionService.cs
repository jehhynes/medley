using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace Medley.Application.Services;

/// <summary>
/// Service for extracting fragments from source content using AI
/// </summary>
public class FragmentExtractionService
{
    private readonly IAiProcessingService _aiService;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IRepository<Fragment> _fragmentRepository;
    private readonly IRepository<Template> _templateRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly TextChunkingService _chunkingService;
    private readonly ILogger<FragmentExtractionService> _logger;

    public FragmentExtractionService(
        IAiProcessingService aiService,
        IRepository<Source> sourceRepository,
        IRepository<Fragment> fragmentRepository,
        IRepository<Template> templateRepository,
        IRepository<Organization> organizationRepository,
        TextChunkingService chunkingService,
        ILogger<FragmentExtractionService> logger)
    {
        _aiService = aiService;
        _sourceRepository = sourceRepository;
        _fragmentRepository = fragmentRepository;
        _templateRepository = templateRepository;
        _organizationRepository = organizationRepository;
        _chunkingService = chunkingService;
        _logger = logger;
    }

    /// <summary>
    /// Extracts fragments from a source using AI
    /// </summary>
    /// <param name="sourceId">The source ID to extract fragments from</param>
    /// <returns>Number of fragments extracted</returns>
    public async Task<int> ExtractFragmentsAsync(Guid sourceId)
    {
        _logger.LogInformation("Starting fragment extraction for source {SourceId}", sourceId);

        // Load source with content
        var source = await _sourceRepository.Query()
            .Include(s => s.Integration)
            .FirstOrDefaultAsync(s => s.Id == sourceId);

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

        // Retrieve the fragment extraction prompt template
        var template = await _templateRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == "FragmentExtraction");

        if (template == null)
        {
            _logger.LogError("FragmentExtraction template not found in database");
            throw new InvalidOperationException("FragmentExtraction template not configured");
        }

        // Get organization's company context (assuming single-tenant for now)
        var organization = await _organizationRepository.Query()
            .FirstOrDefaultAsync();

        // Build system prompt with company context if available
        var systemPrompt = template.Content;
        if (organization != null && !string.IsNullOrWhiteSpace(organization.CompanyContext))
        {
            systemPrompt = $@"{template.Content}

## Company Context
The following context about the company/organization should be considered when extracting fragments:

{organization.CompanyContext}";
            _logger.LogInformation("Including company context in fragment extraction prompt");
        }

        // Chunk the content before processing
        var contentChunks = await _chunkingService.ChunkTextAsync(source.Content);
        _logger.LogInformation("Content chunked into {ChunkCount} chunks for processing", contentChunks.Count);

        // Process each chunk and aggregate fragments
        var allFragments = new List<FragmentDto>();
        var chunkNumber = 0;

        foreach (var chunk in contentChunks)
        {
            chunkNumber++;
            _logger.LogInformation("Processing chunk {ChunkNumber} of {TotalChunks} (length: {ChunkLength} characters)",
                chunkNumber, contentChunks.Count, chunk.Length);

            try
            {
                // Add context about chunking to the system prompt for multi-chunk processing
                var chunkSystemPrompt = contentChunks.Count > 1
                    ? $"{systemPrompt}\n\nNote: This is chunk {chunkNumber} of {contentChunks.Count}. Extract fragments from this portion of the content. Focus on fragments that are complete within this chunk."
                    : systemPrompt;

                var extractionResponse = await _aiService.ProcessStructuredPromptAsync<FragmentExtractionResponse>(
                    userPrompt: chunk,
                    systemPrompt: chunkSystemPrompt);

                if (extractionResponse != null && extractionResponse.Fragments != null && extractionResponse.Fragments.Count > 0)
                {
                    allFragments.AddRange(extractionResponse.Fragments);
                    _logger.LogInformation("Extracted {Count} fragments from chunk {ChunkNumber}",
                        extractionResponse.Fragments.Count, chunkNumber);
                }
                else
                {
                    _logger.LogWarning("No fragments extracted from chunk {ChunkNumber}", chunkNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process chunk {ChunkNumber} of {TotalChunks} for source {SourceId}. Continuing with remaining chunks.",
                    chunkNumber, contentChunks.Count, sourceId);
                // Continue processing other chunks even if one fails
            }
        }

        if (allFragments.Count == 0)
        {
            _logger.LogWarning("No fragments were extracted from any chunk for source {SourceId}", sourceId);
            throw new InvalidOperationException("No fragments could be extracted from the content");
        }

        _logger.LogInformation("Aggregated {TotalCount} fragments from {ChunkCount} chunks",
            allFragments.Count, contentChunks.Count);

        // Create Fragment entities and save them
        var fragmentCount = 0;
        foreach (var fragmentDto in allFragments)
        {
            try
            {
                var fragment = new Fragment
                {
                    Title = fragmentDto.Title?.Trim().Substring(0, Math.Min(200, fragmentDto.Title.Trim().Length)),
                    Summary = fragmentDto.Summary?.Trim().Substring(0, Math.Min(500, fragmentDto.Summary.Trim().Length)),
                    Category = fragmentDto.Category?.Trim().Substring(0, Math.Min(100, fragmentDto.Category.Trim().Length)),
                    Content = fragmentDto.Content?.Trim().Substring(0, Math.Min(10000, fragmentDto.Content.Trim().Length)) 
                        ?? string.Empty,
                    Source = source,
                    LastModifiedAt = DateTimeOffset.UtcNow
                };

                await _fragmentRepository.SaveAsync(fragment);
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
            fragmentCount, sourceId);

        return fragmentCount;
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
    [Description("Clear, descriptive heading")]
    public string? Title { get; set; }
    [Description("Short, human-readable condensation of the full content")]
    public string? Summary { get; set; }
    [Description("Which of the supplied categories this content applies to")]
    public string? Category { get; set; }
    [Description("Markdown-formatted content")]
    public string? Content { get; set; }
}

