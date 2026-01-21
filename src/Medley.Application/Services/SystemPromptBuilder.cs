using System.Text;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Services;

/// <summary>
/// Service for composing structured system prompts from various context sources
/// </summary>
public class SystemPromptBuilder
{
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<AiPrompt> _templateRepository;
    private readonly ILogger<SystemPromptBuilder> _logger;

    public SystemPromptBuilder(
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IRepository<AiPrompt> templateRepository,
        ILogger<SystemPromptBuilder> logger)
    {
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _templateRepository = templateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Build a complete system prompt from article, plan, and template context
    /// </summary>
    /// <param name="articleId">Required: The article being worked on</param>
    /// <param name="planId">Optional: Include plan-specific context if provided</param>
    /// <param name="templateType">Optional: Include template content if provided</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete system prompt string</returns>
    public async Task<string> BuildPromptAsync(
        Guid articleId,
        Guid? planId = null,
        TemplateType? templateType = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building system prompt for article {ArticleId}, plan {PlanId}, template {TemplateType}",
            articleId, planId, templateType);

        var sb = new StringBuilder();

        // 1. Start with template (if provided)
        if (templateType.HasValue)
        {
            var template = await _templateRepository.Query()
                .FirstOrDefaultAsync(t => t.Type == templateType.Value, cancellationToken);

            if (template != null)
            {
                sb.AppendLine(template.Content);
                sb.AppendLine();
            }
            else
            {
                _logger.LogWarning("Template type {TemplateType} not found", templateType);
            }
        }

        // 2. Always append article context (required)
        var article = await _articleRepository.Query()
            .Include(a => a.ArticleType)
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article == null)
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        sb.AppendLine("## Current Article");
        sb.AppendLine();
        sb.AppendLine($"**Title:** {article.Title}");
        
        if (article.ArticleType != null)
        {
            sb.AppendLine($"**Type:** {article.ArticleType.Name}");
        }
        
        if (!string.IsNullOrWhiteSpace(article.Summary))
        {
            sb.AppendLine($"**Summary:** {article.Summary}");
        }
        
        sb.AppendLine($"**Status:** {article.Status}");
        sb.AppendLine();
        sb.AppendLine("**Current Content:**");
        sb.AppendLine();
        sb.AppendLine(article.Content ?? "(No content yet)");
        sb.AppendLine();

        // 3. Conditionally append plan context (if provided)
        if (planId.HasValue)
        {
            var plan = await _planRepository.Query()
                .Include(p => p.PlanFragments.Where(pf => pf.Include))
                    .ThenInclude(pf => pf.Fragment)
                        .ThenInclude(f => f.Source)
                .FirstOrDefaultAsync(p => p.Id == planId.Value, cancellationToken);

            if (plan != null)
            {
                sb.AppendLine("## Improvement Plan");
                sb.AppendLine();
                sb.AppendLine("**Instructions:**");
                sb.AppendLine();
                sb.AppendLine(plan.Instructions);
                sb.AppendLine();

                var includedFragments = plan.PlanFragments.Where(pf => pf.Include).ToList();
                
                if (includedFragments.Any())
                {
                    sb.AppendLine("**Included Fragments:**");
                    sb.AppendLine();

                    foreach (var pf in includedFragments.OrderByDescending(x => x.SimilarityScore))
                    {
                        var fragment = pf.Fragment;
                        var similarityPercent = Math.Round(pf.SimilarityScore * 100, 1);

                        sb.AppendLine($"### {fragment.Title ?? "Untitled Fragment"} (Similarity: {similarityPercent}%)");
                        sb.AppendLine();

                        if (!string.IsNullOrWhiteSpace(fragment.Summary))
                        {
                            sb.AppendLine($"**Summary:** {fragment.Summary}");
                            sb.AppendLine();
                        }

                        if (!string.IsNullOrWhiteSpace(pf.Instructions))
                        {
                            sb.AppendLine($"**Instructions for this fragment:** {pf.Instructions}");
                            sb.AppendLine();
                        }

                        if (fragment.Source != null)
                        {
                            sb.AppendLine($"**Source:** {fragment.Source.Name} ({fragment.Source.Type})");
                            sb.AppendLine();
                        }

                        sb.AppendLine("**Content:**");
                        sb.AppendLine();
                        sb.AppendLine(fragment.Content);
                        sb.AppendLine();
                    }
                }
            }
            else
            {
                _logger.LogWarning("Plan {PlanId} not found", planId);
            }
        }

        // Replace any template placeholders with actual values
        var prompt = sb.ToString();
        prompt = prompt.Replace("{article.Title}", article.Title);
        prompt = prompt.Replace("{article.Content}", article.Content ?? string.Empty);
        prompt = prompt.Replace("{article.Summary}", article.Summary ?? string.Empty);
        prompt = prompt.Replace("{article.Status}", article.Status.ToString());
        
        if (article.ArticleType != null)
        {
            prompt = prompt.Replace("{article.Type}", article.ArticleType.Name);
        }

        _logger.LogInformation("Built system prompt of {Length} characters for article {ArticleId}",
            prompt.Length, articleId);

        return prompt;
    }
}
