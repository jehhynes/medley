using Hangfire;
using Medley.Application.Hubs;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Infrastructure;
using Medley.Infrastructure.Data;
using Medley.Web.Filters;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System.Text.Json.Serialization;
using UoN.ExpressiveAnnotations.Net8.DependencyInjection;

namespace Medley.Web;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/medley-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting Medley application");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog
            builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Add SignalR
            builder.Services.AddSignalR();

            // Configure ASP.NET Core Identity with custom User entity and roles
            builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure cookie authentication
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromDays(14); // Default expiration for "Remember Me"
                options.SlidingExpiration = true;
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
            });

            // Configure Identity options for persistent cookies
            builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromMinutes(30);
            });

            // Configure authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("EditorOrAdmin", policy => policy.RequireRole("Admin", "Editor"));
                options.AddPolicy("ViewerOrHigher", policy => policy.RequireRole("Admin", "Editor", "Viewer"));
            });

            // OAuth 2.0 configuration placeholder for future integrations
            // Uncomment and configure when adding external providers (Fellow.ai, GitHub, etc.)
            /*
            builder.Services.AddAuthentication()
                .AddOAuth("ExternalProvider", options =>
                {
                    options.ClientId = builder.Configuration["OAuth:ClientId"];
                    options.ClientSecret = builder.Configuration["OAuth:ClientSecret"];
                    options.AuthorizationEndpoint = "https://provider.com/oauth/authorize";
                    options.TokenEndpoint = "https://provider.com/oauth/token";
                    options.UserInformationEndpoint = "https://provider.com/oauth/userinfo";
                    options.CallbackPath = "/signin-external";
                });
            */

            var mvc = builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<UnitOfWorkActionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            
            // Configure ExpressiveAnnotations
            builder.Services.AddExpressiveAnnotations();
            
#if DEBUG
            mvc.AddRazorRuntimeCompilation();
#endif

            // Add health checks
            var healthChecksBuilder = builder.Services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();

            // Add AWS health checks only when not in test environment and AWS credentials are available
            var awsSettings = builder.Configuration.GetSection("AWS").Get<Medley.Application.Configuration.AwsSettings>();
            var hasCredentials = !string.IsNullOrEmpty(awsSettings?.AccessKeyId) && !string.IsNullOrEmpty(awsSettings?.SecretAccessKey);
            var isTestEnvironment = builder.Environment.EnvironmentName == "Testing";

            if (hasCredentials && !isTestEnvironment)
            {
                healthChecksBuilder
                    .AddCheck<Medley.Infrastructure.HealthChecks.S3HealthCheck>("aws-s3", tags: new[] { "aws", "s3", "storage" })
                    .AddCheck<Medley.Infrastructure.HealthChecks.BedrockHealthCheck>("aws-bedrock", tags: new[] { "aws", "bedrock", "ai" });
            }

            var app = builder.Build();

            // Apply migrations automatically in development
            if (app.Environment.IsDevelopment())
            {
                app.Services.ApplyMigrations();
            }

            // Initialize recurring jobs
            var jobRegistry = app.Services.GetRequiredService<IJobRegistry>();
            jobRegistry.InitializeRecurringJobs();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // Configure Hangfire dashboard with authentication
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
                DashboardTitle = "Medley Background Jobs",
                DisplayStorageConnectionString = false
            });

            // Map health check endpoint
            app.MapHealthChecks("/health");

            // Map SignalR hubs
            app.MapHub<AdminHub>("/adminHub");
            app.MapHub<ArticleHub>("/articleHub");

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
