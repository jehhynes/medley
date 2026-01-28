using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Medley.Application.Services;

/// <summary>
/// Handles AI-driven tagging of sources and internal/external determination.
/// </summary>
public class TaggingService : ITaggingService
{
    private readonly IRepository<Source> _sourceRepository;
    private readonly IRepository<TagType> _tagTypeRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly ISourceMetadataProvider _metadataProvider;
    private readonly IAiProcessingService _aiProcessingService;
    private readonly ILogger<TaggingService> _logger;
    private readonly AiCallContext _aiCallContext;

    public TaggingService(
        IRepository<Source> sourceRepository,
        IRepository<TagType> tagTypeRepository,
        IRepository<Tag> tagRepository,
        IRepository<Organization> organizationRepository,
        ISourceMetadataProvider metadataProvider,
        IAiProcessingService aiProcessingService,
        ILogger<TaggingService> logger,
        AiCallContext aiCallContext)
    {
        _sourceRepository = sourceRepository;
        _tagTypeRepository = tagTypeRepository;
        _tagRepository = tagRepository;
        _organizationRepository = organizationRepository;
        _metadataProvider = metadataProvider;
        _aiProcessingService = aiProcessingService;
        _logger = logger;
        _aiCallContext = aiCallContext;
    }

    public async Task<TaggingResult> GenerateTagsAsync(Guid sourceId, bool force = false, CancellationToken cancellationToken = default)
    {
        var (emailDomain, tagTypes) = await GetOrganizationContextAsync(cancellationToken);

        var source = await _sourceRepository.Query()
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagType)
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagOption)
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source == null)
        {
            return new TaggingResult
            {
                SourceId = sourceId,
                Processed = false,
                SkipReason = $"Source {sourceId} not found"
            };
        }

        if (!force && source.TagsGenerated.HasValue)
        {
            return new TaggingResult
            {
                SourceId = source.Id,
                Processed = false,
                SkipReason = "Tags already generated"
            };
        }

        var result = await ProcessSourceAsync(source, emailDomain, tagTypes, cancellationToken);
        
        return result;
    }

    private async Task<TaggingResult> ProcessSourceAsync(Source source, string? emailDomain, List<TagType> tagTypes, CancellationToken cancellationToken = default)
    {
        SmartTagResponse? response = null;

        source.IsInternal = DetermineIsInternal(source, emailDomain);

        try
        {
            response = await GenerateSmartTagsAsync(source, emailDomain, tagTypes, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI smart tagging failed for source {SourceId}", source.Id);
        }

        if (response?.Tags != null && response.Tags.Count > 0 && tagTypes.Count > 0)
        {
            await UpsertTagsAsync(source, response.Tags, tagTypes, cancellationToken);
        }

        // Update source scope based on tag types' ScopeUpdateMode
        if (source.IsInternal == null)
        {
            var tagsWithScopeUpdate = source.Tags
                .Where(t => t.TagType != null && t.TagType.ScopeUpdateMode != ScopeUpdateMode.None)
                .ToList();

            if (tagsWithScopeUpdate.Any())
            {
                // Check for MarkExternalIfUnknown first (takes precedence if multiple)
                var hasExternalTag = tagsWithScopeUpdate.Any(t => t.TagType!.ScopeUpdateMode == ScopeUpdateMode.MarkExternalIfUnknown);
                if (hasExternalTag)
                {
                    source.IsInternal = false;
                }
                else
                {
                    // Check for MarkInternalIfUnknown
                    var hasInternalTag = tagsWithScopeUpdate.Any(t => t.TagType!.ScopeUpdateMode == ScopeUpdateMode.MarkInternalIfUnknown);
                    if (hasInternalTag)
                    {
                        source.IsInternal = true;
                    }
                }
            }
        }

        source.TagsGenerated = DateTimeOffset.UtcNow;
        

        return new TaggingResult
        {
            SourceId = source.Id,
            Processed = true,
            IsInternal = source.IsInternal,
            TagCount = source.Tags.Count,
            Message = response?.Message
        };
    }

    private async Task<(string? EmailDomain, List<TagType> TagTypes)> GetOrganizationContextAsync(CancellationToken cancellationToken = default)
    {
        var organization = await _organizationRepository.Query().SingleAsync(cancellationToken);

        var emailDomain = organization.EmailDomain?.Trim();

        var tagTypes = await _tagTypeRepository.Query()
            .Include(t => t.AllowedValues)
            .ToListAsync(cancellationToken);

        if (tagTypes.Count == 0)
        {
            _logger.LogInformation("No tag types configured. Tagging will still attempt IsInternal determination.");
        }

        return (emailDomain, tagTypes);
    }

    private async Task<SmartTagResponse?> GenerateSmartTagsAsync(Source source, string? emailDomain, List<TagType> tagTypes, CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(TaggingService), nameof(GenerateSmartTagsAsync), nameof(Source), source.Id))
        {
            var systemPrompt = BuildSystemPrompt(tagTypes);
            var userPrompt = BuildUserPrompt(source);

            return await _aiProcessingService.ProcessStructuredPromptAsync<SmartTagResponse>(
                userPrompt: userPrompt,
                systemPrompt: systemPrompt,
                cancellationToken: cancellationToken);
        }
    }

    private string BuildSystemPrompt(List<TagType> tagTypes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Tagging Task");
        sb.AppendLine("Your task is to analyze the Source Context and assign **one value** for **each tag type**.");
        sb.AppendLine();
        sb.AppendLine("## Tag Types");
        sb.AppendLine("_Provide exactly one value per tag type._");
        foreach (var tagType in tagTypes)
        {
            var allowedValues = tagType.AllowedValues?.Select(v => v.Value).ToList() ?? new List<string>();
            var constraintLabel = tagType.IsConstrained ? "CONSTRAINED (must pick from allowed values)" : "SUGGESTED (prefer suggested values when they fit)";

            sb.AppendLine();
            sb.AppendLine($"### Tag Type: {tagType.Name}");
            sb.AppendLine($"- Type: {tagType.Name}");
            sb.AppendLine($"- Mode: {constraintLabel}");
            if (allowedValues.Count > 0)
            {
                sb.AppendLine($"- {(tagType.IsConstrained ? "Allowed" : "Suggested")} values: {string.Join(", ", allowedValues)}");
            }

            if (!string.IsNullOrWhiteSpace(tagType.Prompt))
            {
                sb.AppendLine(tagType.Prompt);
            }
        }

        return sb.ToString();
    }

    private string BuildUserPrompt(Source source)
    {
        var folderPath = _metadataProvider.GetFolders(source);
        var existingTags = source.Tags?.ToList() ?? new List<Tag>();

        var sb = new StringBuilder();
        sb.AppendLine("## Source Context");
        if (!string.IsNullOrEmpty(source.Name))
            sb.AppendLine($"- Name: {source.Name}");
        if (folderPath != null && folderPath.Count > 0)
            sb.AppendLine($"- Folder path: `/{string.Join("/", folderPath)}`");

        sb.AppendLine();

        if (existingTags.Count > 0)
        {
            sb.AppendLine("## Existing Tags");
            foreach (var tag in existingTags)
            {
                sb.AppendLine($"- {tag.TagType?.Name ?? tag.TagTypeId.ToString()}: {tag.Value}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private bool? DetermineIsInternal(Source source, string? emailDomain)
    {
        if (emailDomain == null)
            return null;

        var normalizedDomain = emailDomain.StartsWith("@", StringComparison.OrdinalIgnoreCase)
            ? emailDomain.Substring(1)
            : emailDomain;

        var attendeeEmails = _metadataProvider.GetAttendeeEmails(source);

        if (attendeeEmails.Count > 0)
        {
            var allInternal = attendeeEmails.All(email =>
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var emailParts = email.Split('@');
                if (emailParts.Length != 2)
                    return false;

                var attendeeDomain = emailParts[1].Trim();
                return string.Equals(attendeeDomain, normalizedDomain, StringComparison.OrdinalIgnoreCase);
            });

            return allInternal;
        }

        if (!string.IsNullOrWhiteSpace(source.Name))
        {
            if (source.Name.Contains("INTERNAL", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return null;
    }

    private async Task UpsertTagsAsync(Source source, List<SmartTag> tags, List<TagType> tagTypes, CancellationToken cancellationToken = default)
    {
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag.TagType) || string.IsNullOrWhiteSpace(tag.Value))
                continue;

            var tagType = tagTypes.FirstOrDefault(t =>
                string.Equals(t.Name, tag.TagType, StringComparison.OrdinalIgnoreCase));

            if (tagType == null)
            {
                _logger.LogDebug("Ignoring tag with unknown tag type {TagType} for source {SourceId}", tag.TagType, source.Id);
                continue;
            }

            TagOption? matchedOption = null;
            if (tagType.IsConstrained)
            {
                matchedOption = tagType.AllowedValues.FirstOrDefault(o =>
                    string.Equals(o.Value, tag.Value, StringComparison.OrdinalIgnoreCase));

                if (matchedOption == null)
                {
                    _logger.LogWarning("Tag value {Value} is not allowed for constrained tag type {TagType} on source {SourceId}",
                        tag.Value, tagType.Name, source.Id);
                    continue;
                }
            }
            else
            {
                matchedOption = tagType.AllowedValues.FirstOrDefault(o =>
                    string.Equals(o.Value, tag.Value, StringComparison.OrdinalIgnoreCase));
            }

            var valueToStore = tag.Value.Trim();
            if (valueToStore.Length > 200)
            {
                valueToStore = valueToStore.Substring(0, 200);
            }

            var existingTag = source.Tags.FirstOrDefault(t => t.TagTypeId == tagType.Id);

            if (existingTag != null)
            {
                existingTag.Value = valueToStore;
                existingTag.TagOptionId = matchedOption?.Id;
                existingTag.TagOption = matchedOption;
                existingTag.TagType = tagType;

                
            }
            else
            {
                var newTag = new Tag
                {
                    SourceId = source.Id,
                    Source = source,
                    TagTypeId = tagType.Id,
                    TagType = tagType,
                    TagOptionId = matchedOption?.Id,
                    TagOption = matchedOption,
                    Value = valueToStore
                };

                await _tagRepository.AddAsync(newTag);
                source.Tags.Add(newTag);
            }
        }
    }
}

public class SmartTagResponse
{
    public string? Message { get; set; }
    public List<SmartTag> Tags { get; set; } = new();
}

public class SmartTag
{
    public string TagType { get; set; } = string.Empty;
    public string? Value { get; set; }
}

