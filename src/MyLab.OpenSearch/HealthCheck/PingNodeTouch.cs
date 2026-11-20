using MyLab.Log.Dsl;
using OpenSearch.Client;
using OpenSearch.Net;

namespace MyLab.OpenSearch.HealthCheck;

class PingNodeTouch(Node node, IOpenSearchClient client, IDslLogger? log = null) : INodeTouch
{
    public async Task<TouchedNode> TouchAsync(CancellationToken ct)
    {
        var res = await TouchAsyncCore(ct);
        return new TouchedNode(node, res);
    }

    async Task<NodeTouchResult> TouchAsyncCore(CancellationToken ct)
    {
        var request = new PingRequest
        {
            RequestConfiguration = new RequestConfiguration
            {
                ForceNode = node.Uri,
                DisablePing = true,
                MaxRetries = 0,
                RequestTimeout = TimeSpan.FromSeconds(5)
            }
        };

        PingResponse response;

        try
        {
            response = await client.PingAsync(request, ct);
        }
        catch (Exception e)
        {
            log?.Error("HealthCheck unexpected check error: ping exception", e)
                .AndFactIs("node-url", node.Uri)
                .Write();

            return NodeTouchResult.AnotherProblem;
        }

        if (response.IsValid)
            return NodeTouchResult.Ok;

        var connectionError = response.OriginalException is HttpRequestException
        {
            HttpRequestError: HttpRequestError.ConnectionError
        };

        if (connectionError)
            return NodeTouchResult.ConnectionError;

        if (response.ApiCall.HttpStatusCode == 401 || response.ApiCall.HttpStatusCode == 403)
            return NodeTouchResult.RightsNotEnough;

        log?.Error("HealthCheck unexpected check error: unexpected response", response.OriginalException)
            .AndFactIs("node-url", node.Uri)
            .AndFactIs("debug", response.DebugInformation)
            .Write();

        return NodeTouchResult.AnotherProblem;
    }
}