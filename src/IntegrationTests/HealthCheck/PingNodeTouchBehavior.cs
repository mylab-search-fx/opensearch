using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MyLab.Log.Dsl;
using MyLab.OpenSearch.HealthCheck;
using OpenSearch.Client;
using OpenSearch.Net;
using Xunit.Abstractions;

namespace IntegrationTests.HealthCheck;

[Collection(OpenSearchCollection.Name)]
public class PingNodeTouchBehavior(ITestOutputHelper output, GlobalTestFixture fxt)
{
    [Fact]
    public async Task ShouldConnectionErrorWhenNodeUnavailable()
    {
        //Arrange
        var sp = TestTools.CreateApp(output, TestTools.ConfigureDisconnectedHost);
        var c = sp.GetRequiredService<IOpenSearchClient>();
        var node = c.ConnectionSettings.ConnectionPool.Nodes.First();
        var logger = sp.GetRequiredService<ILogger<PingNodeTouchBehavior>>().Dsl();

        var touch = new PingNodeTouch(node, c, logger);

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

        var touch = new PingNodeTouch(node, c, logger);

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

        var touch = new PingNodeTouch(node, c, logger);

        //Act
        var result = await touch.TouchAsync(CancellationToken.None);

        //Assert
        Assert.Equal(NodeTouchResult.RightsNotEnough, result.Result);
    }

    [Fact]
    public async Task ShouldDegradedWhenUnexpected()
    {
        //Arrange
        var clientMock = new Mock<IOpenSearchClient>();
        clientMock.Setup
        (c => c.PingAsync
            (
                It.IsAny<PingRequest>(),
                It.IsAny<CancellationToken>()
            )
        ).Throws<PingRequest, CancellationToken, Exception>((_,_) => new Exception("Expected error"));

        var c = clientMock.Object;
        var node = new Node(new Uri("http://foo.bar"));

        var touch = new PingNodeTouch(node, c);

        //Act
        var result = await touch.TouchAsync(CancellationToken.None);

        //Assert
        Assert.Equal(NodeTouchResult.AnotherProblem, result.Result);
    }
}