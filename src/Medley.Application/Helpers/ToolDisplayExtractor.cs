using System.Text.Json;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Helpers;

/// <summary>
/// Service for extracting display-friendly text from tool call arguments
/// </summary>
public class ToolDisplayExtractor
{
    private readonly IRepository<KnowledgeUnit> _knowledgeUnitRepository;
    private readonly ILogger<ToolDisplayExtractor> _logger;

    public ToolDisplayExtractor(
        IRepository<KnowledgeUnit> knowledgeUnitRepository,
        ILogger<ToolDisplayExtractor> logger)
    {
        _knowledgeUnitRepository = knowledgeUnitRepository;
        _logger = logger;
    }

    /// <summary>
    /// Extract relevant tool display text for the UI
    /// </summary>
    /// <param name="toolName">Name of the tool being called</param>
    /// <param name="arguments">Tool arguments dictionary</param>
    /// <returns>Display-friendly text string, or null if no relevant display text</returns>
    public async Task<string?> ExtractToolDisplayAsync(string? toolName, IDictionary<string, object?>? arguments)
    {
        if (string.IsNullOrWhiteSpace(toolName) || arguments == null || arguments.Count == 0)
        {
            return null;
        }

        try
        {
            // Extract specific properties based on tool name
            return toolName switch
            {
                "SearchKnowledgeUnits" when arguments.TryGetValue("query", out var query) => query?.ToString(),
                "GetKnowledgeUnitContent" when arguments.TryGetValue("knowledgeUnitId", out var knowledgeUnitIdObj) 
                    => await ExtractKnowledgeUnitDisplayAsync(knowledgeUnitIdObj),
                "AskQuestionWithCursor" when arguments.TryGetValue("question", out var question) =>
                question?.ToString(),
                _ => null
            };
        }
        catch (Exception ex)
        {
            // Log but don't fail - just return null
            System.Diagnostics.Debug.WriteLine($"Failed to extract tool display: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts result IDs from tool execution results based on tool name.
    /// </summary>
    public List<Guid>? ExtractResultIds(string? toolName, Microsoft.Extensions.AI.FunctionResultContent resultContent)
    {
        object? result = resultContent.Result;

        if (string.IsNullOrEmpty(toolName) || result == null)
        {
            return null;
        }

        try
        {
            var resultString = result.ToString();
            if (string.IsNullOrEmpty(resultString))
            {
                return null;
            }

            // Check if this is an error result
            if (IsErrorResult(resultString))
            {
                return null;
            }

            var jsonDoc = JsonDocument.Parse(resultString);
            var root = jsonDoc.RootElement;

            // Check if the result was successful
            if (root.TryGetProperty("success", out var successProp) && 
                successProp.ValueKind == JsonValueKind.False)
            {
                return null;
            }

            var ids = new List<Guid>();

            // Extract IDs based on tool name (exact match)
            if (string.Equals(toolName, "CreatePlan", StringComparison.OrdinalIgnoreCase))
            {
                // Extract planId from CreatePlan result
                if (root.TryGetProperty("planId", out var planIdProp) && 
                    planIdProp.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(planIdProp.GetString(), out var planId))
                {
                    ids.Add(planId);
                }
            }
            else if (string.Equals(toolName, "CreateArticleVersion", StringComparison.OrdinalIgnoreCase))
            {
                // Extract versionId from CreateArticleVersion result
                if (root.TryGetProperty("versionId", out var versionIdProp) && 
                    versionIdProp.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(versionIdProp.GetString(), out var versionId))
                {
                    ids.Add(versionId);
                }
            }
            else if (string.Equals(toolName, "SearchKnowledgeUnits", StringComparison.OrdinalIgnoreCase) || 
                     string.Equals(toolName, "FindSimilarKnowledgeUnits", StringComparison.OrdinalIgnoreCase))
            {
                // Extract knowledge unit IDs from search results
                if (root.TryGetProperty("knowledgeUnits", out var knowledgeUnitsProp) && 
                    knowledgeUnitsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var knowledgeUnit in knowledgeUnitsProp.EnumerateArray())
                    {
                        if (knowledgeUnit.TryGetProperty("id", out var idProp) && 
                            idProp.ValueKind == JsonValueKind.String &&
                            Guid.TryParse(idProp.GetString(), out var knowledgeUnitId))
                        {
                            ids.Add(knowledgeUnitId);
                        }
                    }
                }
            }
            else if (string.Equals(toolName, "GetKnowledgeUnitContent", StringComparison.OrdinalIgnoreCase))
            {
                // Extract knowledge unit ID from GetKnowledgeUnitContent result
                if (root.TryGetProperty("knowledgeUnit", out var knowledgeUnitProp) &&
                    knowledgeUnitProp.TryGetProperty("id", out var idProp) &&
                    idProp.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(idProp.GetString(), out var knowledgeUnitId))
                {
                    ids.Add(knowledgeUnitId);
                }
            }
            else if (string.Equals(toolName, "AskQuestionWithCursor", StringComparison.OrdinalIgnoreCase))
            {
                // For Cursor questions, we don't extract IDs but we want to mark it as having a result
                // Return an empty list to indicate the tool completed successfully
                // The frontend will handle displaying the response differently
                return new List<Guid>();
            }

            return ids.Count > 0 ? ids : null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool result JSON for tool {ToolName}", toolName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting result IDs from tool {ToolName}", toolName);
            return null;
        }
    }

    /// <summary>
    /// Checks if a FunctionResultContent represents an error.
    /// </summary>
    public bool IsErrorResult(Microsoft.Extensions.AI.FunctionResultContent resultContent)
    {
        if (resultContent?.Result == null)
        {
            return false;
        }

        var resultString = resultContent.Result.ToString();
        return IsErrorResult(resultString);
    }

    /// <summary>
    /// Checks if a result string represents an error.
    /// </summary>
    private bool IsErrorResult(string? resultString)
    {
        if (string.IsNullOrEmpty(resultString))
        {
            return false;
        }

        // Check for common error patterns
        if (resultString.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Try to parse as JSON and check for error indicators
        try
        {
            var jsonDoc = JsonDocument.Parse(resultString);
            var root = jsonDoc.RootElement;

            // Check for success: false
            if (root.TryGetProperty("success", out var successProp) && 
                successProp.ValueKind == JsonValueKind.False)
            {
                return true;
            }

            // Check for error property
            if (root.TryGetProperty("error", out var errorProp) && 
                errorProp.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(errorProp.GetString()))
            {
                return true;
            }
        }
        catch (JsonException)
        {
            // Not valid JSON, rely on string pattern matching
        }

        return false;
    }

    private async Task<string?> ExtractKnowledgeUnitDisplayAsync(object? knowledgeUnitIdObj)
    {
        if (knowledgeUnitIdObj == null)
        {
            return null;
        }

        // Try to parse the knowledge unit ID
        Guid knowledgeUnitId;
        if (knowledgeUnitIdObj is Guid guid)
        {
            knowledgeUnitId = guid;
        }
        else if (Guid.TryParse(knowledgeUnitIdObj.ToString(), out var parsedGuid))
        {
            knowledgeUnitId = parsedGuid;
        }
        else
        {
            return null;
        }

        // Look up the knowledge unit title
        try
        {
            var title = await _knowledgeUnitRepository.Query()
                .Where(ku => ku.Id == knowledgeUnitId)
                .Select(ku => ku.Title)
                .FirstOrDefaultAsync();

            return title ?? knowledgeUnitId.ToString();
        }
        catch
        {
            // Fallback to showing the ID
            return knowledgeUnitId.ToString();
        }
    }
}
