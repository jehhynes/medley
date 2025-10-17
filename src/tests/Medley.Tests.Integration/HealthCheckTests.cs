using Xunit;

namespace Medley.Tests.Integration;

public class HealthCheckTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public HealthCheckTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HealthCheck_WithRealDatabase_ReturnsHealthy()
    {
        // Arrange - Integration tests use shared database (no isolation needed for health checks)
        using var factory = new CustomWebApplicationFactory(_fixture);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }
}