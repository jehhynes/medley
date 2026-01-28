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
    private readonly IRepository<AiModelCostParameter> _costParameterRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TokenUsageApiController(
        IRepository<AiTokenUsage> tokenUsageRepository,
        IRepository<AiModelCostParameter> costParameterRepository,
        IRepository<Organization> organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _costParameterRepository = costParameterRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<TokenUsageMetrics>> GetMetrics()
    {
        // Get organization timezone
        var organization = await _organizationRepository.Query().FirstOrDefaultAsync();
        var timeZoneId = organization?.TimeZone ?? "America/New_York";

        var metrics = new TokenUsageMetrics();
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(nowUtc, timeZoneId);
        var thirtyDaysAgoLocal = nowLocal.AddDays(-30).Date;

        // Daily usage (last 30 days) - grouped in database by local date
        var dailyUsageData = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .Select(t => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(t.Timestamp.UtcDateTime, timeZoneId).Date,
                t.InputTokens,
                t.OutputTokens,
                t.EmbeddingTokens
            })
            .Where(t => t.LocalDate >= thirtyDaysAgoLocal)
            .GroupBy(t => t.LocalDate)
            .Select(g => new
            {
                DateKey = g.Key,
                InputTokens = g.Sum(x => x.InputTokens ?? 0),
                OutputTokens = g.Sum(x => x.OutputTokens ?? 0),
                EmbeddingTokens = g.Sum(x => x.EmbeddingTokens ?? 0)
            })
            .ToListAsync();

        // Convert DateTime to string after loading from database
        metrics.DailyUsage = dailyUsageData
            .Select(d => new DailyTokenUsage
            {
                Date = d.DateKey.ToString("yyyy-MM-dd"),
                InputTokens = d.InputTokens,
                OutputTokens = d.OutputTokens,
                EmbeddingTokens = d.EmbeddingTokens
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Fill in missing days with zero values
        var filledDailyUsage = new List<DailyTokenUsage>();
        for (int i = 29; i >= 0; i--)
        {
            var date = nowLocal.AddDays(-i).Date;
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

        // All time by type - aggregated in database
        var allTimeByType = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .GroupBy(t => 1) // Group all records together
            .Select(g => new TokensByType
            {
                InputTokens = g.Sum(x => x.InputTokens ?? 0),
                OutputTokens = g.Sum(x => x.OutputTokens ?? 0),
                EmbeddingTokens = g.Sum(x => x.EmbeddingTokens ?? 0)
            })
            .FirstOrDefaultAsync();

        metrics.AllTimeByType = allTimeByType ?? new TokensByType();

        // Last 30 days by type - aggregated in database
        var last30DaysByType = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .Select(t => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(t.Timestamp.UtcDateTime, timeZoneId).Date,
                t.InputTokens,
                t.OutputTokens,
                t.EmbeddingTokens
            })
            .Where(t => t.LocalDate >= thirtyDaysAgoLocal)
            .GroupBy(t => 1) // Group all records together
            .Select(g => new TokensByType
            {
                InputTokens = g.Sum(x => x.InputTokens ?? 0),
                OutputTokens = g.Sum(x => x.OutputTokens ?? 0),
                EmbeddingTokens = g.Sum(x => x.EmbeddingTokens ?? 0)
            })
            .FirstOrDefaultAsync();

        metrics.Last30DaysByType = last30DaysByType ?? new TokensByType();

        // All time by service - aggregated in database
        metrics.AllTimeByService = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .GroupBy(t => t.ServiceName)
            .Select(g => new MetricItem
            {
                Label = g.Key,
                Count = (int)(g.Sum(x => (x.InputTokens ?? 0) + (x.OutputTokens ?? 0) + (x.EmbeddingTokens ?? 0)))
            })
            .OrderByDescending(m => m.Count)
            .ToListAsync();

        // Last 30 days by service - aggregated in database
        metrics.Last30DaysByService = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .Select(t => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(t.Timestamp.UtcDateTime, timeZoneId).Date,
                t.ServiceName,
                t.InputTokens,
                t.OutputTokens,
                t.EmbeddingTokens
            })
            .Where(t => t.LocalDate >= thirtyDaysAgoLocal)
            .GroupBy(t => t.ServiceName)
            .Select(g => new MetricItem
            {
                Label = g.Key,
                Count = (int)(g.Sum(x => (x.InputTokens ?? 0) + (x.OutputTokens ?? 0) + (x.EmbeddingTokens ?? 0)))
            })
            .OrderByDescending(m => m.Count)
            .ToListAsync();

        return Ok(metrics);
    }

    /// <summary>
    /// Get all distinct models with their token type capabilities
    /// </summary>
    [HttpGet("models")]
    public async Task<ActionResult<List<ModelInfo>>> GetModels()
    {
        var models = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .GroupBy(t => t.ModelName)
            .Select(g => new ModelInfo
            {
                ModelName = g.Key,
                HasInputTokens = g.Any(t => t.InputTokens != null && t.InputTokens > 0),
                HasOutputTokens = g.Any(t => t.OutputTokens != null && t.OutputTokens > 0),
                HasEmbeddingTokens = g.Any(t => t.EmbeddingTokens != null && t.EmbeddingTokens > 0)
            })
            .OrderBy(m => m.ModelName)
            .ToListAsync();

        return Ok(models);
    }

    /// <summary>
    /// Get all cost parameters
    /// </summary>
    [HttpGet("cost-parameters")]
    public async Task<ActionResult<List<ModelCostParameter>>> GetCostParameters()
    {
        var parameters = await _costParameterRepository.Query()
            .Select(p => new ModelCostParameter
            {
                Id = p.Id,
                ModelName = p.ModelName,
                InputCostPerMillion = p.InputCostPerMillion,
                OutputCostPerMillion = p.OutputCostPerMillion,
                EmbeddingCostPerMillion = p.EmbeddingCostPerMillion
            })
            .OrderBy(p => p.ModelName)
            .ToListAsync();

        return Ok(parameters);
    }

    /// <summary>
    /// Create or update cost parameters for a model
    /// </summary>
    [HttpPost("cost-parameters")]
    public async Task<ActionResult<ModelCostParameter>> SaveCostParameter([FromBody] SaveCostParameterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ModelName))
        {
            return BadRequest("ModelName is required");
        }

        // Check if parameter already exists
        var existing = await _costParameterRepository.Query()
            .FirstOrDefaultAsync(p => p.ModelName == request.ModelName);

        if (existing != null)
        {
            // Update existing
            existing.InputCostPerMillion = request.InputCostPerMillion;
            existing.OutputCostPerMillion = request.OutputCostPerMillion;
            existing.EmbeddingCostPerMillion = request.EmbeddingCostPerMillion;
            
            // No explicit update needed - EF tracks changes
            await _unitOfWork.SaveChangesAsync();

            return Ok(new ModelCostParameter
            {
                Id = existing.Id,
                ModelName = existing.ModelName,
                InputCostPerMillion = existing.InputCostPerMillion,
                OutputCostPerMillion = existing.OutputCostPerMillion,
                EmbeddingCostPerMillion = existing.EmbeddingCostPerMillion
            });
        }
        else
        {
            // Create new
            var newParameter = new AiModelCostParameter
            {
                ModelName = request.ModelName,
                InputCostPerMillion = request.InputCostPerMillion,
                OutputCostPerMillion = request.OutputCostPerMillion,
                EmbeddingCostPerMillion = request.EmbeddingCostPerMillion
            };

            await _costParameterRepository.AddAsync(newParameter);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new ModelCostParameter
            {
                Id = newParameter.Id,
                ModelName = newParameter.ModelName,
                InputCostPerMillion = newParameter.InputCostPerMillion,
                OutputCostPerMillion = newParameter.OutputCostPerMillion,
                EmbeddingCostPerMillion = newParameter.EmbeddingCostPerMillion
            });
        }
    }

    /// <summary>
    /// Delete cost parameters for a model
    /// </summary>
    [HttpDelete("cost-parameters/{id}")]
    public async Task<ActionResult> DeleteCostParameter(Guid id)
    {
        var parameter = await _costParameterRepository.GetByIdAsync(id);
        if (parameter == null)
        {
            return NotFound();
        }

        await _costParameterRepository.DeleteAsync(parameter);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get cost estimates for the last 30 days
    /// </summary>
    [HttpGet("cost-estimates")]
    public async Task<ActionResult<CostEstimateMetrics>> GetCostEstimates()
    {
        // Get organization timezone
        var organization = await _organizationRepository.Query().FirstOrDefaultAsync();
        var timeZoneId = organization?.TimeZone ?? "America/New_York";

        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(nowUtc, timeZoneId);
        var thirtyDaysAgoLocal = nowLocal.AddDays(-30).Date;

        // Get all cost parameters
        var costParameters = await _costParameterRepository.Query()
            .ToDictionaryAsync(p => p.ModelName);

        // Get token usage for last 30 days with local date grouping - done in database
        var tokenUsageByDateAndModel = await _tokenUsageRepository.Query()
            .Where(t => t.IsSuccess)
            .Select(t => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(t.Timestamp.UtcDateTime, timeZoneId).Date,
                t.ModelName,
                t.InputTokens,
                t.OutputTokens,
                t.EmbeddingTokens
            })
            .Where(t => t.LocalDate >= thirtyDaysAgoLocal)
            .ToListAsync(); // Minimal data loaded - only what we need

        // Calculate daily costs
        var dailyCosts = tokenUsageByDateAndModel
            .GroupBy(t => t.LocalDate)
            .Select(g =>
            {
                decimal inputCost = 0;
                decimal outputCost = 0;
                decimal embeddingCost = 0;

                foreach (var item in g)
                {
                    if (costParameters.TryGetValue(item.ModelName, out var costParam))
                    {
                        if (item.InputTokens.HasValue && costParam.InputCostPerMillion.HasValue)
                        {
                            inputCost += (item.InputTokens.Value / 1_000_000m) * costParam.InputCostPerMillion.Value;
                        }

                        if (item.OutputTokens.HasValue && costParam.OutputCostPerMillion.HasValue)
                        {
                            outputCost += (item.OutputTokens.Value / 1_000_000m) * costParam.OutputCostPerMillion.Value;
                        }

                        if (item.EmbeddingTokens.HasValue && costParam.EmbeddingCostPerMillion.HasValue)
                        {
                            embeddingCost += (item.EmbeddingTokens.Value / 1_000_000m) * costParam.EmbeddingCostPerMillion.Value;
                        }
                    }
                }

                return new DailyCostEstimate
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    InputCost = Math.Round(inputCost, 2),
                    OutputCost = Math.Round(outputCost, 2),
                    EmbeddingCost = Math.Round(embeddingCost, 2),
                    TotalCost = Math.Round(inputCost + outputCost + embeddingCost, 2)
                };
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Fill in missing days with zero values
        var filledDailyCosts = new List<DailyCostEstimate>();
        for (int i = 29; i >= 0; i--)
        {
            var date = nowLocal.AddDays(-i).Date;
            var dateStr = date.ToString("yyyy-MM-dd");
            var existing = dailyCosts.FirstOrDefault(d => d.Date == dateStr);

            filledDailyCosts.Add(existing ?? new DailyCostEstimate
            {
                Date = dateStr,
                InputCost = 0,
                OutputCost = 0,
                EmbeddingCost = 0,
                TotalCost = 0
            });
        }

        // Find models with usage but no cost parameters
        var modelsWithUsage = tokenUsageByDateAndModel
            .Select(t => t.ModelName)
            .Distinct()
            .ToList();

        var missingCostParameters = modelsWithUsage
            .Where(m => !costParameters.ContainsKey(m))
            .OrderBy(m => m)
            .ToList();

        var result = new CostEstimateMetrics
        {
            DailyCosts = filledDailyCosts,
            TotalEstimatedCost = Math.Round(filledDailyCosts.Sum(d => d.TotalCost), 2),
            MissingCostParameters = missingCostParameters
        };

        return Ok(result);
    }
}
