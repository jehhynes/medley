using Medley.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Medley.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Medley.Web.Program>
{
    private readonly DatabaseFixture _fixture;

    public TestWebApplicationFactory(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext and NpgsqlDataSource registrations
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dataSourceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(NpgsqlDataSource));
            if (dataSourceDescriptor != null)
            {
                services.Remove(dataSourceDescriptor);
            }

            // Register test data source as singleton
            services.AddSingleton<NpgsqlDataSource>(sp =>
                ApplicationDbContextFactory.CreateDataSource(_fixture.ConnectionString));

            // Add DbContext using the test data source
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options.UseNpgsql(dataSource, o => o.UseVector());
            });
        });
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Database.BeginTransactionAsync();
    }
}