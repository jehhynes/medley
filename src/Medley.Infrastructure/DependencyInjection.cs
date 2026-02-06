using Amazon;
using Amazon.BedrockRuntime;
using Amazon.S3;
using Hangfire;
using Hangfire.Console;
using Hangfire.MissionControl;
using Hangfire.PostgreSql;
using Hangfire.RecurringJobCleanUpManager;
using Medley.Application;
using Medley.Application.Configuration;
using Medley.Application.Enums;
using Medley.Application.Helpers;
using Medley.Application.Integrations.Interfaces;
using Medley.Application.Integrations.Services;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Infrastructure.Data;
using Medley.Infrastructure.Data.Repositories;
using Medley.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using OllamaSharp;
using OpenAI;

namespace Medley.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Register NpgsqlDataSource as singleton for proper connection pool management
        services.AddSingleton<NpgsqlDataSource>(sp => ApplicationDbContextFactory.CreateDataSource(connectionString));

        // Register DbContext using the singleton data source
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            options
                .UseNpgsql(dataSource, o => o.UseVector())
                .UseSnakeCaseNamingConvention();
        });

        // Configure Data Protection to persist keys to database
        services.AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>()
            .SetApplicationName("Medley");

        // Register repositories and unit of work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IFragmentRepository, FragmentRepository>();
        services.AddScoped<IRepository<Fragment>>(sp => sp.GetRequiredService<IFragmentRepository>());
        services.AddScoped<IKnowledgeUnitRepository, KnowledgeUnitRepository>();
        services.AddScoped<IRepository<KnowledgeUnit>>(sp => sp.GetRequiredService<IKnowledgeUnitRepository>());
        services.AddScoped<ISourceRepository, SourceRepository>();
        services.AddScoped<IRepository<Source>>(sp => sp.GetRequiredService<ISourceRepository>());
        services.AddScoped<IObservationRepository, ObservationRepository>();
        services.AddScoped<IFindingRepository, FindingRepository>();
        services.AddScoped<IInsightRepository, InsightRepository>();
        services.AddScoped<IClusterRepository, ClusterRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register application services
        services.AddScoped<IUserAuditLogService, UserAuditLogService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddScoped<IKnowledgeBuilderImportService, KnowledgeBuilderImportService>();
        services.AddScoped<ICollectorImportService, CollectorImportService>();
        services.AddScoped<ISourceMetadataProvider, SourceMetadataProvider>();
        services.AddScoped<ITaggingService, TaggingService>();
        services.AddScoped<IContentChunkingService, ContentChunkingService>();
        services.AddScoped<IEmbeddingHelper, EmbeddingHelper>();
        services.AddScoped<IArticleVersionService, ArticleVersionService>();
        services.AddScoped<FragmentExtractionService>();
        services.AddScoped<KMeansBucketingService>();
        services.AddScoped<IFragmentClusteringService, FragmentClusteringService>();
        
        // Register AI call context for token usage tracking
        services.AddScoped<AiCallContext>();
        
        //services.AddScoped<IntegrationHealthCheckJob>();
        services.AddTransient<KnowledgeUnitGeneratorJob>();
        services.AddTransient<ArticleChatJob>();
        services.AddTransient<FragmentClusteringJob>();
        services.AddSingleton<RecurringJobCleanUpManager>();
        services.AddSingleton<IJobRegistry, JobRegistry>();

        // Register HttpClient for integration services
        services.AddHttpClient();

        // Register integration connection services
        services.AddScoped<IIntegrationConnectionService, GitHubIntegrationService>();
        services.AddScoped<IIntegrationConnectionService, FellowIntegrationService>();

        // Register concrete integration services for direct injection
        services.AddScoped<FellowIntegrationService>();
        services.AddScoped<GitHubIntegrationService>();

        // Configure Hangfire
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            })
            .UseMissionControl(typeof(BaseHangfireJob<>).Assembly)
            .UseConsole()
        );

        // Register Hangfire servers (skip in test environment)
        if (!environment.IsTesting())
        {
            // Default queue server for background processing
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 6; //Environment.ProcessorCount * 2;
                options.Queues = new[] { "default" };
                options.ServerName = "Background";
                //options.ServerTimeout = TimeSpan.FromMinutes(4);
                //options.ServerCheckInterval = TimeSpan.FromMinutes(1);
                //options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
            });

            // Dedicated UI queue server for user-facing operations (chat, notifications)
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 8; 
                options.Queues = new[] { "ui" };
                options.ServerName = "UI";
                // More responsive settings for UI jobs
                //options.ServerTimeout = TimeSpan.FromMinutes(2);
                //options.ServerCheckInterval = TimeSpan.FromSeconds(30);
                //options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
            });
        }

        // Configure AWS services
        ConfigureAwsServices(services, builder);

        // Configure embedding service (Ollama or OpenAI)
        ConfigureEmbeddingServices(services, configuration);

        return services;
    }

    private static void ConfigureAwsServices(IServiceCollection services, IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        // Bind configuration classes
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));
        services.Configure<AwsSettings>(configuration.GetSection("AWS"));
        services.Configure<S3Settings>(configuration.GetSection("AWS:S3"));
        services.Configure<BedrockSettings>(configuration.GetSection("AWS:Bedrock"));

        // Get AWS settings for service registration
        var awsSettings = configuration.GetSection("AWS").Get<AwsSettings>() ?? new AwsSettings();
        var fileStorageSettings = configuration.GetSection("FileStorage").Get<FileStorageSettings>() ?? new FileStorageSettings();

        // Register AWS clients only when credentials are available and not in test environment
        var hasCredentials = !string.IsNullOrEmpty(awsSettings.AccessKeyId) && !string.IsNullOrEmpty(awsSettings.SecretAccessKey);
        
        if (hasCredentials && !environment.IsTesting())
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(awsSettings.Region),
                };

                if (hasCredentials)
                {
                    return new AmazonS3Client(awsSettings.AccessKeyId, awsSettings.SecretAccessKey, config);
                }
                else
                {
                    // Use default credential chain (environment variables, IAM roles, etc.)
                    return new AmazonS3Client(config);
                }
            });

            services.AddSingleton<AmazonBedrockRuntimeClient>(sp =>
            {
                var config = new AmazonBedrockRuntimeConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(awsSettings.Region),
                };

                config.Timeout = TimeSpan.FromSeconds(awsSettings.Bedrock.TimeoutSeconds);

                if (hasCredentials)
                {
                    return new AmazonBedrockRuntimeClient(awsSettings.AccessKeyId, awsSettings.SecretAccessKey, config);
                }
                else
                {
                    // Use default credential chain (environment variables, IAM roles, etc.)
                    return new AmazonBedrockRuntimeClient(config);
                }
            });
        }

        // Register file storage service based on provider
        switch (fileStorageSettings.Provider)
        {
            case FileStorageProvider.Local:
                services.AddScoped<IFileStorageService, LocalFileStorageService>();
                break;
            case FileStorageProvider.S3:
                services.AddScoped<IFileStorageService, S3FileStorageService>();
                break;
            default:
                throw new InvalidOperationException($"Unsupported file storage provider: {fileStorageSettings.Provider}");
        }

        // Register AI processing service only when AWS clients are available
        if (hasCredentials && !environment.IsTesting())
        {
            // Register IChatClient with token tracking middleware (shared by all services)
            services.AddScoped<IChatClient>(sp =>
            {
                var bedrockClient = sp.GetRequiredService<AmazonBedrockRuntimeClient>();
                var bedrockSettings = sp.GetRequiredService<IOptions<BedrockSettings>>();
                
                // Create base ChatClient from Bedrock client
                var baseChatClient = bedrockClient.AsIChatClient(bedrockSettings.Value.ModelId);
                
                // Wrap with token tracking middleware
                var aiCallContext = sp.GetRequiredService<AiCallContext>();
                var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var middlewareLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Middleware.TokenTrackingChatClientMiddleware>>();
                
                return new Application.Middleware.TokenTrackingChatClientMiddleware(
                    baseChatClient,
                    aiCallContext,
                    serviceScopeFactory,
                    middlewareLogger,
                    bedrockSettings.Value.ModelId);
            });
            
            // Register BedrockAiService (uses registered IChatClient)
            //services.AddScoped<IAiProcessingService>(sp =>
            //{
            //    var chatClient = sp.GetRequiredService<IChatClient>();
            //    var bedrockSettings = sp.GetRequiredService<IOptions<BedrockSettings>>();
            //    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BedrockAiService>>();
                
            //    return new BedrockAiService(chatClient, bedrockSettings, logger);
            //});

            services.AddScoped<IAiProcessingService, BedrockAiService>();

            // Configure chat service
            ConfigureChatService(services, configuration);
        }

        // Note: AWS health checks are registered in Program.cs conditionally
    }

    private static void ConfigureChatService(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind Bedrock settings for chat service
        services.Configure<BedrockSettings>(configuration.GetSection("AWS:Bedrock"));

        // Register Cursor settings and service
        services.Configure<CursorSettings>(configuration.GetSection("Cursor"));
        services.AddScoped<ICursorService, CursorService>();

        // Register ArticleAssistantPluginsFactory (for creating article-scoped plugin instances)
        services.AddScoped<ArticleChatToolsFactory>();

        // Register SystemContextManager (for composing system prompts from article, plan, and template context)
        services.AddScoped<SystemContextManager>();

        // Register ToolDisplayExtractor (for extracting display-friendly tool text)
        services.AddScoped<ToolDisplayExtractor>();

        // Register ArticleChatService (uses registered IChatClient and wraps it with function invocation)
        services.AddScoped<IArticleChatService, ArticleChatService>();
    }

    private static void ConfigureEmbeddingServices(IServiceCollection services, IConfiguration configuration)
    {
        // Bind embedding configuration
        services.Configure<EmbeddingSettings>(configuration.GetSection("Embedding"));
        
        // Get embedding settings from configuration
        var embeddingSettings = configuration.GetSection("Embedding").Get<EmbeddingSettings>() ?? new EmbeddingSettings();
        
        // Register IEmbeddingGenerator<string, Embedding<float>> based on the configured provider
        // Wrapped with TokenTrackingEmbeddingGenerator for usage tracking
        services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var provider = embeddingSettings.Provider?.ToLowerInvariant() ?? "ollama";
            
            var innerGenerator = provider switch
            {
                "openai" => ConfigureOpenAIEmbedding(embeddingSettings),
                "ollama" => ConfigureOllamaEmbedding(embeddingSettings),
                _ => throw new InvalidOperationException($"Unsupported embedding provider: {embeddingSettings.Provider}")
            };
            
            // Determine model name for tracking
            var modelName = provider switch
            {
                "openai" => embeddingSettings.OpenAI.Model,
                "ollama" => embeddingSettings.Ollama.Model,
                _ => "unknown"
            };
            
            // Wrap with token tracking
            var aiCallContext = sp.GetRequiredService<AiCallContext>();
            var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TokenTrackingEmbeddingGenerator>>();
            
            return new TokenTrackingEmbeddingGenerator(
                innerGenerator,
                aiCallContext,
                serviceScopeFactory,
                logger,
                modelName);
        });
    }

    private static IEmbeddingGenerator<string, Embedding<float>> ConfigureOllamaEmbedding(EmbeddingSettings settings)
    {
        var baseUrl = new Uri(settings.Ollama.BaseUrl);
        var ollamaClient = new OllamaApiClient(baseUrl);
        ollamaClient.SelectedModel = settings.Ollama.Model;
        return ollamaClient;
    }

    private static IEmbeddingGenerator<string, Embedding<float>> ConfigureOpenAIEmbedding(EmbeddingSettings settings)
    {
        if (string.IsNullOrEmpty(settings.OpenAI.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required when using OpenAI embedding provider. Please configure Embedding:OpenAI:ApiKey in your appsettings.");
        }

        var openAIClient = new OpenAIClient(settings.OpenAI.ApiKey);

        return openAIClient.GetEmbeddingClient(settings.OpenAI.Model)
            .AsIEmbeddingGenerator();
    }
}