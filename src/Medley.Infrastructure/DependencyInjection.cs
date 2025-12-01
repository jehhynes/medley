using Medley.Application.Integrations.Interfaces;
using Medley.Application.Integrations.Services;
using Medley.Application.Configuration;
using Medley.Application.Enums;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Application.Jobs;
using Hangfire.RecurringJobCleanUpManager;
using Medley.Infrastructure.Data;
using Medley.Infrastructure.Data.Repositories;
using Medley.Infrastructure.Services;
using Medley.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;
using Amazon.S3;
using Amazon.BedrockRuntime;
using Amazon;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Medley.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Register NpgsqlDataSource as singleton for proper connection pool management
        services.AddSingleton<NpgsqlDataSource>(sp => ApplicationDbContextFactory.CreateDataSource(connectionString));

        // Register DbContext using the singleton data source
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource, o => o.UseVector());
        });

        // Register repositories and unit of work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IFragmentRepository, FragmentRepository>();
        services.AddScoped<IObservationRepository, ObservationRepository>();
        services.AddScoped<IFindingRepository, FindingRepository>();
        services.AddScoped<IInsightRepository, InsightRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register application services
        services.AddScoped<IUserAuditLogService, UserAuditLogService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddScoped<IKnowledgeBuilderImportService, KnowledgeBuilderImportService>();
        services.AddScoped<ICollectorImportService, CollectorImportService>();
        //services.AddScoped<IntegrationHealthCheckJob>();
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
            }));

        // Register Hangfire server
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 4; //Environment.ProcessorCount * 2;
            //options.Queues = new[] { "default", "high", "low" };
            //options.ServerTimeout = TimeSpan.FromMinutes(4);
            //options.ServerCheckInterval = TimeSpan.FromMinutes(1);
            //options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });

        // Register background job service
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        // Configure AWS services
        ConfigureAwsServices(services, configuration);

        return services;
    }

    private static void ConfigureAwsServices(IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration classes
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));
        services.Configure<AwsSettings>(configuration.GetSection("AWS"));
        services.Configure<S3Settings>(configuration.GetSection("AWS:S3"));
        services.Configure<BedrockSettings>(configuration.GetSection("AWS:Bedrock"));

        // Get AWS settings for service registration
        var awsSettings = configuration.GetSection("AWS").Get<AwsSettings>() ?? new AwsSettings();
        var fileStorageSettings = configuration.GetSection("FileStorage").Get<FileStorageSettings>() ?? new FileStorageSettings();

        // Register AWS clients only when credentials are available or not in test environment
        var hasCredentials = !string.IsNullOrEmpty(awsSettings.AccessKeyId) && !string.IsNullOrEmpty(awsSettings.SecretAccessKey);
        var isTestEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                               Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Testing";

        if (hasCredentials || !isTestEnvironment)
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(awsSettings.Region),
                    ServiceURL = null // Use default AWS endpoints
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
                    ServiceURL = null // Use default AWS endpoints
                };

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
        if (hasCredentials || !isTestEnvironment)
        {
            services.AddScoped<IAiProcessingService, BedrockAiService>();
        }

        // Note: AWS health checks are registered in Program.cs conditionally
    }
}