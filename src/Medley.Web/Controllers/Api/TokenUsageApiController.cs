using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/token-usage")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize(Roles = "Admin")]
public class TokenUsageApiController : ControllerBase
{
    private readonly IRepository<AiTokenUsage> _tokenUsageRepository;

    public TokenUsageApiController(IRepository<AiTokenUsage> tokenUsageRepository)
    {
        _tokenUsageRepository = tokenUsageRepository;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<TokenUsageMetrics>> GetMetrics()
    {
        var metrics = new TokenUsageMetrics();
        var now = DateTimeOffset.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        // Get all token usage data
        var allTokenUsage = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .Select(t => new
            {
                t.Timestamp,
                t.InputTokens,
                t.OutputTokens,
                t.EmbeddingTokens,
                t.ServiceName
            })
            .ToListAsync();

        // Filter for last 30 days
        var last30DaysUsage = allTokenUsage
            .Where(t => t.Timestamp >= thirtyDaysAgo)
            .ToList();

        // Daily usage (last 30 days) - stacked bar chart data
        metrics.DailyUsage = last30DaysUsage
            .GroupBy(t => t.Timestamp.Date)
            .Select(g => new DailyTokenUsage
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                InputTokens = g.Sum(x => x.InputTokens ?? 0),
                OutputTokens = g.Sum(x => x.OutputTokens ?? 0),
                EmbeddingTokens = g.Sum(x => x.EmbeddingTokens ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Fill in missing days with zero values
        var filledDailyUsage = new List<DailyTokenUsage>();
        for (int i = 29; i >= 0; i--)
        {
            var date = now.AddDays(-i).Date;
            var dateStr = date.ToString("yyyy-MM-dd");
            var existing = metrics.DailyUsage.FirstOrDefault(d => d.Date == dateStr);
            
            filledDailyUsage.Add(existing ?? new DailyTokenUsage
            {
                Date = dateStr,
                InputTokens = 0,
                OutputTokens = 0,
                EmbeddingTokens = 0
            });
        }
        metrics.DailyUsage = filledDailyUsage;

        // All time by type
        metrics.AllTimeByType = new TokensByType
        {
            InputTokens = allTokenUsage.Sum(t => t.InputTokens ?? 0),
            OutputTokens = allTokenUsage.Sum(t => t.OutputTokens ?? 0),
            EmbeddingTokens = allTokenUsage.Sum(t => t.EmbeddingTokens ?? 0)
        };

        // Last 30 days by type
        metrics.Last30DaysByType = new TokensByType
        {
            InputTokens = last30DaysUsage.Sum(t => t.InputTokens ?? 0),
            OutputTokens = last30DaysUsage.Sum(t => t.OutputTokens ?? 0),
            EmbeddingTokens = last30DaysUsage.Sum(t => t.EmbeddingTokens ?? 0)
        };

        // All time by service
        metrics.AllTimeByService = allTokenUsage
            .GroupBy(t => t.ServiceName)
            .Select(g => new MetricItem
            {
                Label = g.Key,
                Count = (int)(g.Sum(x => (x.InputTokens ?? 0) + (x.OutputTokens ?? 0) + (x.EmbeddingTokens ?? 0)))
            })
            .OrderByDescending(m => m.Count)
            .ToList();

        // Last 30 days by service
        metrics.Last30DaysByService = last30DaysUsage
            .GroupBy(t => t.ServiceName)
            .Select(g => new MetricItem
            {
                Label = g.Key,
                Count = (int)(g.Sum(x => (x.InputTokens ?? 0) + (x.OutputTokens ?? 0) + (x.EmbeddingTokens ?? 0)))
            })
            .OrderByDescending(m => m.Count)
            .ToList();

        return Ok(metrics);
    }
}
