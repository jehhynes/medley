using System.IO.Compression;
using System.Text.Json;
using Medley.Application.Interfaces;
using Medley.Application.Models;
using Medley.Domain.Entities;
using Medley.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Infrastructure.Services;

/// <summary>
/// Service for importing Knowledge Builder articles from JSON or ZIP files
/// </summary>
public class KnowledgeBuilderImportService : IKnowledgeBuilderImportService
{
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<ArticleType> _articleTypeRepository;
    private readonly ILogger<KnowledgeBuilderImportService> _logger;

    public KnowledgeBuilderImportService(
        IRepository<Article> articleRepository,
        IRepository<ArticleType> articleTypeRepository,
        ILogger<KnowledgeBuilderImportService> logger)
    {
        _articleRepository = articleRepository;
        _articleTypeRepository = articleTypeRepository;
        _logger = logger;
    }

    public async Task<KnowledgeBuilderImportValidation> ValidateJsonAsync(Stream jsonStream)
    {
        try
        {
            var articles = await DeserializeArticlesAsync(jsonStream);
            
            if (articles == null || articles.Length == 0)
            {
                return KnowledgeBuilderImportValidation.Invalid("No articles found in JSON file");
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            ValidateArticlesRecursive(articles, errors, warnings);

            if (errors.Any())
            {
                return new KnowledgeBuilderImportValidation
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                };
            }

            return new KnowledgeBuilderImportValidation
            {
                IsValid = true,
                ArticleCount = CountArticlesRecursive(articles),
                Warnings = warnings
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed");
            return KnowledgeBuilderImportValidation.Invalid($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            return KnowledgeBuilderImportValidation.Invalid($"Validation error: {ex.Message}");
        }
    }

    public async Task<List<Stream>> ExtractZipAsync(Stream zipStream)
    {
        var jsonStreams = new List<Stream>();

        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var memoryStream = new MemoryStream();
                    using (var entryStream = entry.Open())
                    {
                        await entryStream.CopyToAsync(memoryStream);
                    }
                    memoryStream.Position = 0;
                    jsonStreams.Add(memoryStream);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract ZIP file");
            throw;
        }

        return jsonStreams;
    }

    public async Task<KnowledgeBuilderImportResult> ProcessFileAsync(Stream fileStream, string fileName)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            List<KnowledgeBuilderArticle[]> allArticleSets = new();

            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Processing ZIP file: {FileName}", fileName);
                var jsonStreams = await ExtractZipAsync(fileStream);

                if (!jsonStreams.Any())
                {
                    return KnowledgeBuilderImportResult.FailureResult("No JSON files found in ZIP archive");
                }

                foreach (var jsonStream in jsonStreams)
                {
                    try
                    {
                        var articles = await DeserializeArticlesAsync(jsonStream);
                        if (articles != null && articles.Length > 0)
                        {
                            allArticleSets.Add(articles);
                        }
                    }
                    finally
                    {
                        await jsonStream.DisposeAsync();
                    }
                }
            }
            else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Processing JSON file: {FileName}", fileName);
                var articles = await DeserializeArticlesAsync(fileStream);
                if (articles != null && articles.Length > 0)
                {
                    allArticleSets.Add(articles);
                }
            }
            else
            {
                return KnowledgeBuilderImportResult.FailureResult("Unsupported file type. Only .json and .zip files are supported.");
            }

            if (!allArticleSets.Any())
            {
                return KnowledgeBuilderImportResult.FailureResult("No valid articles found in file");
            }

            // Import all article sets
            var totalImported = 0;
            var totalSkipped = 0;
            var allErrors = new List<string>();
            var allWarnings = new List<string>();

            foreach (var articleSet in allArticleSets)
            {
                var result = await ImportArticlesAsync(articleSet, preserveHierarchy: true);
                totalImported += result.ArticlesImported;
                totalSkipped += result.ArticlesSkipped;
                allErrors.AddRange(result.Errors);
                allWarnings.AddRange(result.Warnings);
            }

            var duration = DateTime.UtcNow - startTime;

            return new KnowledgeBuilderImportResult
            {
                Success = !allErrors.Any(),
                ArticlesImported = totalImported,
                ArticlesSkipped = totalSkipped,
                TotalArticlesProcessed = totalImported + totalSkipped,
                Errors = allErrors,
                Warnings = allWarnings,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {FileName}", fileName);
            return KnowledgeBuilderImportResult.FailureResult($"Processing error: {ex.Message}");
        }
    }

    public async Task<KnowledgeBuilderImportResult> ImportArticlesAsync(KnowledgeBuilderArticle[] articles, bool preserveHierarchy = true)
    {
        var startTime = DateTime.UtcNow;
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Load all article types into memory for quick lookup
            var articleTypes = await _articleTypeRepository.Query().ToListAsync();
            var articleTypeCache = new Dictionary<string, ArticleType>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var type in articleTypes)
            {
                articleTypeCache[type.Name] = type;
            }

            // Process articles recursively
            foreach (var kbArticle in articles)
            {
                try
                {
                    var (articleImported, articleSkipped) = await ImportArticleRecursiveAsync(
                        kbArticle, null, articleTypeCache, errors, warnings);
                    imported += articleImported;
                    skipped += articleSkipped;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import article: {Title}", kbArticle.Title);
                    errors.Add($"Failed to import article '{kbArticle.Title}': {ex.Message}");
                    skipped++;
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Import completed: {Imported} imported, {Skipped} skipped in {Duration}ms", 
                imported, skipped, duration.TotalMilliseconds);

            return new KnowledgeBuilderImportResult
            {
                Success = !errors.Any(),
                ArticlesImported = imported,
                ArticlesSkipped = skipped,
                TotalArticlesProcessed = imported + skipped,
                Errors = errors,
                Warnings = warnings,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed");
            return KnowledgeBuilderImportResult.FailureResult($"Import error: {ex.Message}");
        }
    }

    private async Task<(int imported, int skipped)> ImportArticleRecursiveAsync(
        KnowledgeBuilderArticle kbArticle,
        Guid? parentArticleId,
        Dictionary<string, ArticleType> articleTypeCache,
        List<string> errors,
        List<string> warnings)
    {
        var imported = 0;
        var skipped = 0;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(kbArticle.Title))
        {
            errors.Add("Article title is required");
            return (0, 1);
        }

        // Get or create ArticleType
        ArticleType? articleType = null;
        if (!string.IsNullOrWhiteSpace(kbArticle.Type))
        {
            if (!articleTypeCache.TryGetValue(kbArticle.Type, out articleType))
            {
                // Create new article type
                articleType = new ArticleType
                {
                    Name = kbArticle.Type
                };
                await _articleTypeRepository.Add(articleType);
                articleTypeCache[kbArticle.Type] = articleType;
                _logger.LogInformation("Created new ArticleType: {Type}", kbArticle.Type);
            }
        }

        // Serialize metadata to JSON
        string? metadataJson = null;
        if (kbArticle.Metadata != null)
        {
            metadataJson = JsonSerializer.Serialize(kbArticle.Metadata);
        }

        // Create article
        var article = new Article
        {
            Title = kbArticle.Title,
            Summary = kbArticle.Summary,
            Content = kbArticle.Content,
            Metadata = metadataJson,
            CreatedAt = kbArticle.CreatedAt != default ? kbArticle.CreatedAt : DateTimeOffset.UtcNow,
            ParentArticleId = parentArticleId,
            ArticleTypeId = articleType?.Id,
            Status = Domain.Enums.ArticleStatus.Draft
        };

        await _articleRepository.Add(article);
        imported++;

        _logger.LogDebug("Imported article: {Title} (ID: {Id})", article.Title, article.Id);

        // Recursively import children
        if (kbArticle.Children != null && kbArticle.Children.Any())
        {
            foreach (var child in kbArticle.Children)
            {
                var (childImported, childSkipped) = await ImportArticleRecursiveAsync(
                    child, article.Id, articleTypeCache, errors, warnings);
                imported += childImported;
                skipped += childSkipped;
            }
        }

        return (imported, skipped);
    }

    private async Task<KnowledgeBuilderArticle[]?> DeserializeArticlesAsync(Stream jsonStream)
    {
        jsonStream.Position = 0;
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return await JsonSerializer.DeserializeAsync<KnowledgeBuilderArticle[]>(jsonStream, options);
    }

    private void ValidateArticlesRecursive(KnowledgeBuilderArticle[] articles, List<string> errors, List<string> warnings)
    {
        foreach (var article in articles)
        {
            if (string.IsNullOrWhiteSpace(article.Title))
            {
                errors.Add("Article title is required");
            }

            if (string.IsNullOrWhiteSpace(article.Type))
            {
                warnings.Add($"Article '{article.Title}' has no type specified");
            }

            if (article.Children != null && article.Children.Any())
            {
                ValidateArticlesRecursive(article.Children.ToArray(), errors, warnings);
            }
        }
    }

    private int CountArticlesRecursive(KnowledgeBuilderArticle[] articles)
    {
        var count = articles.Length;
        foreach (var article in articles)
        {
            if (article.Children != null && article.Children.Any())
            {
                count += CountArticlesRecursive(article.Children.ToArray());
            }
        }
        return count;
    }
}

