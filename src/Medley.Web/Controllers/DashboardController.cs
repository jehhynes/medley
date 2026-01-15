using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IRepository<Source> _sourceRepository;
        private readonly IRepository<Fragment> _fragmentRepository;
        private readonly IRepository<Article> _articleRepository;
        private readonly IRepository<ArticleType> _articleTypeRepository;
        private readonly IRepository<Tag> _tagRepository;

        public DashboardController(
            IRepository<Source> sourceRepository,
            IRepository<Fragment> fragmentRepository,
            IRepository<Article> articleRepository,
            IRepository<ArticleType> articleTypeRepository,
            IRepository<Tag> tagRepository)
        {
            _sourceRepository = sourceRepository;
            _fragmentRepository = fragmentRepository;
            _articleRepository = articleRepository;
            _articleTypeRepository = articleTypeRepository;
            _tagRepository = tagRepository;
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<DashboardMetrics>> GetMetrics()
        {
            var metrics = new DashboardMetrics();

            // Source metrics
            metrics.TotalSources = await _sourceRepository.Query().CountAsync();
            metrics.SourcesByType = await _sourceRepository.Query()
                .GroupBy(s => s.Type)
                .Select(g => new MetricItem { Label = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();
            
            metrics.SourcesByIntegration = await _sourceRepository.Query()
                .Include(s => s.Integration)
                .GroupBy(s => s.Integration.Type)
                .Select(g => new MetricItem { Label = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Temporal analytics - Stacked by Internal/External
            var sourcesByYearData = await _sourceRepository.Query()
                .Select(s => new { Year = s.Date.Year, s.IsInternal })
                .ToListAsync();
            
            metrics.SourcesByYear = sourcesByYearData
                .GroupBy(s => s.Year)
                .Select(g => new StackedMetricItem
                {
                    Label = g.Key.ToString(),
                    Values = new Dictionary<string, int>
                    {
                        { "Internal", g.Count(x => x.IsInternal == true) },
                        { "External", g.Count(x => x.IsInternal == false) },
                        { "Unknown", g.Count(x => x.IsInternal == null) }
                    }
                })
                .OrderBy(m => m.Label)
                .ToList();

            var monthlyData = await _sourceRepository.Query()
                .Select(s => s.Date)
                .GroupBy(d => new { d.Year, d.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();
            
            metrics.SourcesByMonth = monthlyData
                .Select(x => new MetricItem { Label = $"{x.Year}-{x.Month:D2}", Count = x.Count })
                .ToList();

            // Tag analytics
            var tags = await _tagRepository.Query()
                .Include(t => t.TagType)
                .Select(t => new { TypeName = t.TagType.Name, t.Value, SourceId = t.Source.Id })
                .ToListAsync();

            var tagMetricsList = new List<TagTypeMetrics>();
            var typeGroups = tags.GroupBy(t => t.TypeName);

            foreach (var typeGroup in typeGroups)
            {
                var rawCounts = typeGroup.GroupBy(x => x.Value)
                    .Select(vg => new MetricItem 
                    { 
                        Label = vg.Key, 
                        Count = vg.Select(x => x.SourceId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                if (rawCounts.Count <= 20)
                {
                    tagMetricsList.Add(new TagTypeMetrics 
                    { 
                        TagTypeName = typeGroup.Key, 
                        TagCounts = rawCounts 
                    });
                }
                else
                {
                    // Top 20 Chart (Top 19 + Others)
                    var top20 = rawCounts.Take(19).ToList();
                    var othersCount = rawCounts.Skip(19).Sum(x => x.Count);
                    if (othersCount > 0)
                    {
                        top20.Add(new MetricItem { Label = "Others", Count = othersCount });
                    }
                    tagMetricsList.Add(new TagTypeMetrics
                    {
                        TagTypeName = $"{typeGroup.Key} (Top 20)",
                        TagCounts = top20
                    });

                    // Consolidated Chart
                    var prefixCounts = rawCounts
                        .Select(x => x.Label.Split(' ')[0])
                        .GroupBy(p => p)
                        .ToDictionary(g => g.Key, g => g.Count());

                    var consolidated = rawCounts
                        .GroupBy(x => {
                            var prefix = x.Label.Split(' ')[0];
                            return prefixCounts[prefix] > 1 ? prefix : x.Label;
                        })
                        .Select(g => new MetricItem
                        {
                            Label = g.Key,
                            Count = g.Sum(x => x.Count)
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    var consolidatedTop10 = consolidated.Take(10).ToList();
                    var consolidatedOthers = consolidated.Skip(10).Sum(x => x.Count);
                    if (consolidatedOthers > 0)
                    {
                        consolidatedTop10.Add(new MetricItem { Label = "Others", Count = consolidatedOthers });
                    }
                    tagMetricsList.Add(new TagTypeMetrics
                    {
                        TagTypeName = $"{typeGroup.Key} (Consolidated)",
                        TagCounts = consolidatedTop10
                    });
                }
            }

            metrics.SourcesByTagType = tagMetricsList.OrderBy(x => x.TagTypeName).ToList();
            
            metrics.SourcesPendingSmartTagging = await _sourceRepository.Query()
                .CountAsync(s => s.TagsGenerated == null);
            
            metrics.SourcesPendingFragmentGeneration = await _sourceRepository.Query()
                .CountAsync(s => s.ExtractionStatus == Domain.Enums.ExtractionStatus.NotStarted);

            // Fragment metrics
            metrics.TotalFragments = await _fragmentRepository.Query().CountAsync();
            metrics.FragmentsByCategory = await _fragmentRepository.Query()
                .Where(f => f.Category != null)
                .GroupBy(f => f.Category)
                .Select(g => new MetricItem { Label = g.Key ?? "Unknown", Count = g.Count() })
                .ToListAsync();

            // Populate icons for fragment categories
            var articleTypes = await _articleTypeRepository.Query().ToListAsync();
            foreach (var item in metrics.FragmentsByCategory)
            {
                var normalizedCategory = new string(item.Label.Where(char.IsLetter).ToArray()).ToLower();
                
                if (normalizedCategory == "bestpractice")
                {
                    item.Icon = "shield-check";
                    continue;
                }

                var match = articleTypes.FirstOrDefault(at => 
                    new string(at.Name.Where(char.IsLetter).ToArray()).ToLower() == normalizedCategory);
                
                if (match != null)
                {
                    item.Icon = match.Icon;
                }
                else
                {
                    item.Icon = "file-text";
                }
            }
            
            metrics.FragmentsPendingEmbedding = await _fragmentRepository.Query()
                .CountAsync(f => f.Embedding == null);

            // Article metrics
            metrics.TotalArticles = await _articleRepository.Query().CountAsync();
            metrics.ArticlesByType = await _articleRepository.Query()
                .Include(a => a.ArticleType)
                .Where(a => a.ArticleType != null)
                .GroupBy(a => new { a.ArticleType!.Name, a.ArticleType.Icon })
                .Select(g => new MetricItem { Label = g.Key.Name, Count = g.Count(), Icon = g.Key.Icon })
                .ToListAsync();
            
            var articlesWithoutType = await _articleRepository.Query()
                .CountAsync(a => a.ArticleType == null);
            if (articlesWithoutType > 0)
            {
                metrics.ArticlesByType.Add(new MetricItem { Label = "Untyped", Count = articlesWithoutType });
            }

            return Ok(metrics);
        }
    }
}
