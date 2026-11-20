using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.OpenSearch.Options;
using MyLab.OpenSearch.Tools;
using OpenSearch.Client;
using OpenSearch.Net;

namespace MyLab.OpenSearch.HealthCheck;

class OpenSearchHealthCheck
    (
        IOpenSearchClient client, 
        IOptions<OpenSearchOptions> opts, 
        ILogger<OpenSearchHealthCheck>? logger = null
    ) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var nodes = client.ConnectionSettings.ConnectionPool.Nodes.ToArray();
        
        var touchedNodes = await PingNodesAsync(nodes, cancellationToken);
        
        var noProblemCount = touchedNodes.Count(n => n.Result == NodeTouchResult.Ok);

        if (noProblemCount == 0)
            return HealthCheckResult.Unhealthy("OpenSearch connection is not established");

        var details = touchedNodes.ToDictionary(
            n => n.Node.Uri.ToString(),
            n => (object)(Enum.GetName(n.Result) ?? string.Empty)
        );

        if (noProblemCount == touchedNodes.Length)
            return HealthCheckResult.Healthy("OpenSearch connection established", details);

        return HealthCheckResult.Degraded(
            $"OpenSearch connection degraded: {noProblemCount}/{touchedNodes.Length}", 
            null,
            details);
    }    

    async Task<TouchedNode[]> PingNodesAsync(Node[] nodes, CancellationToken cancellationToken)
    {
        var pingTasks = nodes.Select(node => PingNodeAsync(node, cancellationToken));
        var pingResults = await Task.WhenAll(pingTasks);
        return pingResults;
    }

    Task<TouchedNode> PingNodeAsync(Node node, CancellationToken cancellationToken)
        => CreateNodeTouch(node).TouchAsync(cancellationToken);

    INodeTouch CreateNodeTouch(Node node)
    {
        return opts.Value.ApiKeyAuth != null
            ? new IndexNodeTouch(node, opts.Value, logger?.Dsl())
            : new PingNodeTouch(node, client, logger?.Dsl());
    }
}

enum NodeTouchResult
{
    Undefined,
    Ok,
    ConnectionError,
    RightsNotEnough,
    AnotherProblem
}

record TouchedNode(Node Node, NodeTouchResult Result);

interface INodeTouch
{
    Task<TouchedNode> TouchAsync(CancellationToken ct);
}

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

        if (response.ApiCall.HttpStatusCode == 401)
            return NodeTouchResult.RightsNotEnough;

        log?.Error("HealthCheck unexpected check error: unexpected response", response.OriginalException)
            .AndFactIs("node-url", node.Uri)
            .AndFactIs("debug", response.DebugInformation)
            .Write();

        return NodeTouchResult.AnotherProblem;
    }
}

class IndexNodeTouch(
    Node node,
    OpenSearchOptions opts,
    Func<IOpenSearchClient, CancellationToken, Task<ISearchResponse<object>>> search,
    IDslLogger? log = null)
    : INodeTouch
{

    public IndexNodeTouch(Node node, OpenSearchOptions opts, IDslLogger? log = null)
        :this(node, opts, SearchCore, log)
    {
        
    }

    public async Task<TouchedNode> TouchAsync(CancellationToken ct)
    {
        var res = await TouchAsyncCore(ct);
        return new TouchedNode(node, res);
    }

    async Task<NodeTouchResult> TouchAsyncCore(CancellationToken ct)
    {
        var tmpOpts = new OpenSearchOptions
        {
            Host = node.Uri.ToString(),
            ApiKeyAuth = opts.ApiKeyAuth,
            BasicAuth = opts.BasicAuth
        };

        var tmpPool = ConnectionPoolFactory.Create(tmpOpts);
        var settings = OpenSearchClientFactory.CreateSettings(opts, tmpPool);
        var client = new OpenSearchClient(settings);

        ISearchResponse<object> response;

        try
        {
            response = await search(client, ct);
        }
        catch (Exception e)
        {
            log?.Error("HealthCheck unexpected check error: request error", e)
                .AndFactIs("node-url", node.Uri)
                .Write();

            return NodeTouchResult.AnotherProblem;
        }

        var connectionError = response.OriginalException is HttpRequestException
        {
            HttpRequestError: HttpRequestError.ConnectionError
        };

        if (connectionError)
            return NodeTouchResult.ConnectionError;

        if (response.ApiCall.HttpStatusCode == 404)
            return NodeTouchResult.Ok;

        if (response.ApiCall.HttpStatusCode == 401)
            return NodeTouchResult.RightsNotEnough;

        log?.Error("HealthCheck unexpected check error: unexpected response", response.OriginalException)
            .AndFactIs("node-url", node.Uri)
            .AndFactIs("debug", response.DebugInformation)
            .Write();

        return NodeTouchResult.AnotherProblem;
    }

    static Task<ISearchResponse<object>> SearchCore(
        IOpenSearchClient client,
        CancellationToken ct)
    => client.SearchAsync<object>(s => s
            .Index(Guid.NewGuid().ToString("N"))
            .Size(0)               // Не тянем сами документы (экономим сеть и память)
            .TrackTotalHits(false) // Не тратим CPU на точный подсчет всех документов
            .Preference("_local")  // Заставляем ноду искать только в своих локальных шардах
            .Query(q => q.MatchAll()),
        ct);
}