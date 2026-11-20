using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyLab.OpenSearch.HealthCheck;
using OpenSearch.Client;
using OpenSearch.Net;
using MyLab.Log.Dsl;
using MyLab.OpenSearch.Options;
using Xunit.Abstractions;

namespace IntegrationTests.HealthCheck;

[Collection(OpenSearchCollection.Name)]
public class IndexNodeTouchBehavior(ITestOutputHelper output, GlobalTestFixture fxt)
{
    [Fact]
    public async Task ShouldConnectionErrorWhenNodeUnavailable()
    {
        //Arrange
        var sp = TestTools.CreateApp(output, TestTools.ConfigureDisconnectedHost);
        var c = sp.GetRequiredService<IOpenSearchClient>();
        var node = c.ConnectionSettings.ConnectionPool.Nodes.First();
        var logger = sp.GetRequiredService<ILogger<PingNodeTouchBehavior>>().Dsl();
        var opts = sp.GetRequiredService<IOptions<OpenSearchOptions>>();

        var touch = new IndexNodeTouch(node, opts.Value, logger);

        //Act
        var result = await touch.TouchAsync(CancellationToken.None);

        //Assert
        Assert.Equal(NodeTouchResult.ConnectionError, result.Result);
    }

    [Fact]
    public async Task ShouldOkWhenNodeAvailable()
    {
        //Arrange
        var sp = TestTools.CreateApp(output, o =>
        {
            TestTools.ConfigureConnectedHost(o);
            o.BasicAuth = TestTools.AdminBasicAuth;
        });
        var c = sp.GetRequiredService<IOpenSearchClient>();
        var node = c.ConnectionSettings.ConnectionPool.Nodes.First();
        var logger = sp.GetRequiredService<ILogger<PingNodeTouchBehavior>>().Dsl();
        var opts = sp.GetRequiredService<IOptions<OpenSearchOptions>>();

        var touch = new IndexNodeTouch(node, opts.Value, logger);

        //Act
        var result = await touch.TouchAsync(CancellationToken.None);

        //Assert
        Assert.Equal(NodeTouchResult.Ok, result.Result);
    }

    [Fact]
    public async Task ShouldDegradedWhenLowRights()
    {
        //Arrange
        var sp = TestTools.CreateApp(output, o =>
        {
            TestTools.ConfigureConnectedHost(o);
            o.ApiKeyAuth = fxt.ApiKey;
        });
        var c = sp.GetRequiredService<IOpenSearchClient>();
        var node = c.ConnectionSettings.ConnectionPool.Nodes.First();
        var logger = sp.GetRequiredService<ILogger<PingNodeTouchBehavior>>().Dsl();
        var opts = sp.GetRequiredService<IOptions<OpenSearchOptions>>();

        var touch = new IndexNodeTouch(node, opts.Value, logger);

        //Act
        var result = await touch.TouchAsync(CancellationToken.None);

        //Assert
        Assert.Equal(NodeTouchResult.RightsNotEnough, result.Result);
    }

    [Fact]
    public async Task ShouldDegradedWhenUnexpected()
    {
        //Arrange
        var node = new Node(new Uri("http://foo.bar"));

        var touch = new IndexNodeTouch(node, new(), (_, _) => Task.FromException<ISearchResponse<object>>(new Exception("Expected error")));

        //Act
        var result = await touch.TouchAsync(CancellationToken.None);

        //Assert
        Assert.Equal(NodeTouchResult.AnotherProblem, result.Result);
    }
}