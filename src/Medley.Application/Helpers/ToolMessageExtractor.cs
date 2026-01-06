using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Medley.Application.Helpers;

/// <summary>
/// Service for extracting display-friendly messages from tool call arguments
/// </summary>
public class ToolMessageExtractor
{
    private readonly IRepository<Fragment> _fragmentRepository;

    public ToolMessageExtractor(IRepository<Fragment> fragmentRepository)
    {
        _fragmentRepository = fragmentRepository;
    }

    /// <summary>
    /// Extract relevant tool message for display in the UI
    /// </summary>
    /// <param name="toolName">Name of the tool being called</param>
    /// <param name="arguments">Tool arguments dictionary</param>
    /// <returns>Display-friendly message string, or null if no relevant message</returns>
    public async Task<string?> ExtractToolMessageAsync(string? toolName, IDictionary<string, object?>? arguments)
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
                "SearchFragments" when arguments.TryGetValue("query", out var query) => query?.ToString(),
                "GetFragmentContent" when arguments.TryGetValue("fragmentId", out var fragmentIdObj) 
                    => await ExtractFragmentMessageAsync(fragmentIdObj),
                _ => null
            };
        }
        catch (Exception ex)
        {
            // Log but don't fail - just return null
            System.Diagnostics.Debug.WriteLine($"Failed to extract tool message: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> ExtractFragmentMessageAsync(object? fragmentIdObj)
    {
        if (fragmentIdObj == null)
        {
            return null;
        }

        // Try to parse the fragment ID
        Guid fragmentId;
        if (fragmentIdObj is Guid guid)
        {
            fragmentId = guid;
        }
        else if (Guid.TryParse(fragmentIdObj.ToString(), out var parsedGuid))
        {
            fragmentId = parsedGuid;
        }
        else
        {
            return null;
        }

        // Look up the fragment title
        try
        {
            var title = await _fragmentRepository.Query()
                .Where(f => f.Id == fragmentId)
                .Select(f => f.Title)
                .FirstOrDefaultAsync();

            return title ?? fragmentId.ToString();
        }
        catch
        {
            // Fallback to showing the ID
            return fragmentId.ToString();
        }
    }
}
