using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit.Abstractions;

namespace IntegrationTests.HealthCheck;

[Collection(OpenSearchCollection.Name)]
public class OpenSearchHealthCheckBehavior(ITestOutputHelper output, GlobalTestFixture fxt)
{
    [Fact]
    public async Task ShouldBeHealthyWhenSingleHostIsConnected()
    {
        // Arrange
        await using var serviceProvider = TestTools.CreateApp(output, o =>
        {
            TestTools.ConfigureConnectedHost(o);
            o.ApiKeyAuth = fxt.ApiKey;
        });
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        var status = report.Entries["opensearch"].Status;

        // Assert
        Assert.Equal(HealthStatus.Healthy, status);
    }

    [Fact]
    public async Task ShouldBeUnhealthyWhenSingleHostIsNotConnected()
    {
        // Arrange
        await using var serviceProvider = TestTools.CreateApp(output, o =>
        {
            TestTools.ConfigureDisconnectedHost(o);
            o.ApiKeyAuth = fxt.ApiKey;
        });
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        var status = report.Entries["opensearch"].Status;

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, status);
    }

    [Fact]
    public async Task ShouldBeDegradedWhenOnlyPartOfHostPoolIsConnected()
    {
        // Arrange
        await using var serviceProvider = TestTools.CreateApp(output, o =>
        {
            TestTools.ConfigureMixedHostPool(o);
            o.ApiKeyAuth = fxt.ApiKey;
        });
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        var status = report.Entries["opensearch"].Status;

        // Assert
        Assert.Equal(HealthStatus.Degraded, status);
    }
}