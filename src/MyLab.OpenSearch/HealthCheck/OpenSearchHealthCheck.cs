using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.OpenSearch.Options;
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

    INodeTouch CreateNodeTouch(Node node) => new PingNodeTouch(node, client, logger?.Dsl());
}