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
    private readonly ILogger<FragmentExtractionService> _logger;

    public FragmentExtractionService(
        IAiProcessingService aiService,
        IRepository<Source> sourceRepository,
        IRepository<Fragment> fragmentRepository,
        IRepository<Template> templateRepository,
        ILogger<FragmentExtractionService> logger)
    {
        _aiService = aiService;
        _sourceRepository = sourceRepository;
        _fragmentRepository = fragmentRepository;
        _templateRepository = templateRepository;
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

        // Call AI service to process the prompt with structured JSON response
        FragmentExtractionResponse extractionResponse;
        try
        {
            extractionResponse = await _aiService.ProcessStructuredPromptAsync<FragmentExtractionResponse>(
                userPrompt: source.Content,
                systemPrompt: template.Content,
                maxTokens: 8000);
            
            if (extractionResponse == null || extractionResponse.Fragments == null)
            {
                _logger.LogError("AI response is null or contains no fragments");
                throw new InvalidOperationException("Invalid AI response format");
            }
            
            _logger.LogInformation("Parsed {Count} fragments from AI response", 
                extractionResponse.Fragments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service failed to process prompt for source {SourceId}", sourceId);
            throw new InvalidOperationException($"AI processing failed: {ex.Message}", ex);
        }

        // Create Fragment entities and save them
        var fragmentCount = 0;
        foreach (var fragmentDto in extractionResponse.Fragments)
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
    [Description("Which of the 7 categories above (or suggest a new category if none apply)")]
    public string? Category { get; set; }
    [Description("Markdown-formatted content")]
    public string? Content { get; set; }
}

