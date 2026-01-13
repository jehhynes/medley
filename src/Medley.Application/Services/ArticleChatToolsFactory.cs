using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Application.Services;

/// <summary>
/// Factory for creating article-scoped ArticleAssistantPlugins instances
/// </summary>
public class ArticleChatToolsFactory
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<PlanFragment> _planFragmentRepository;
    private readonly IArticleVersionService _articleVersionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<EmbeddingSettings> _embeddingSettings;
    private readonly AiCallContext _aiCallContext;

    public ArticleChatToolsFactory(
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IRepository<PlanFragment> planFragmentRepository,
        IArticleVersionService articleVersionService,
        IUnitOfWork unitOfWork,
        ILoggerFactory loggerFactory,
        IOptions<EmbeddingSettings> embeddingSettings,
        AiCallContext aiCallContext)
    {
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _planFragmentRepository = planFragmentRepository;
        _articleVersionService = articleVersionService;
        _unitOfWork = unitOfWork;
        _loggerFactory = loggerFactory;
        _embeddingSettings = embeddingSettings;
        _aiCallContext = aiCallContext;
    }

    /// <summary>
    /// Creates a new ArticleAssistantPlugins instance scoped to a specific article
    /// </summary>
    /// <param name="articleId">The article ID to scope the plugins to</param>
    /// <param name="userId">The user ID of the current user</param>
    /// <param name="implementingPlanId">Optional plan ID if implementing a plan</param>
    /// <param name="conversationId">Optional conversation ID for tracking the conversation</param>
    /// <returns>A new ArticleAssistantPlugins instance</returns>
    public ArticleChatTools Create(Guid articleId, Guid userId, Guid? implementingPlanId = null, Guid? conversationId = null)
    {
        var logger = _loggerFactory.CreateLogger<ArticleChatTools>();
        
        return new ArticleChatTools(
            articleId,
            userId,
            implementingPlanId,
            conversationId,
            _fragmentRepository,
            _embeddingGenerator,
            _articleRepository,
            _planRepository,
            _planFragmentRepository,
            _articleVersionService,
            _unitOfWork,
            logger,
            _embeddingSettings,
            _aiCallContext);
    }
}
