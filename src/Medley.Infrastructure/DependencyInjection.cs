using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Infrastructure.Data;
using Medley.Infrastructure.Data.Repositories;
using Medley.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;

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

        return services;
    }
}