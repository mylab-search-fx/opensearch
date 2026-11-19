using Microsoft.Extensions.DependencyInjection;
using MyLab.OpenSearch.HealthCheck;
using OpenSearch.Client;
using Xunit.Abstractions;

namespace IntegrationTests.HealthCheck;

[Collection(OpenSearchCollection.Name)]
public class PingNodeTouchBehavior(ITestOutputHelper output, GlobalTestFixture fxt)
{
    [Fact]
    public async Task ShouldReturnConnectionErrorWhenNodeUnavailable()
    {
        //Arrange
        var sp = TestTools.CreateApp(output, TestTools.ConfigureDisconnectedHost);
        var c = sp.GetRequiredService<IOpenSearchClient>();
        var node = c.ConnectionSettings.ConnectionPool.Nodes.First();


        var touch = new PingNodeTouch(node, c);

        //Act
        var result = await touch.TouchAsync(CancellationToken.None);

        //Assert
        Assert.Equal(NodeTouchResult.ConnectionError, result);
    }
}