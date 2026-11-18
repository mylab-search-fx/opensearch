using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenSearch.Client;
using OpenSearch.Net;

namespace MyLab.OpenSearch.HealthCheck
{
    class OpenSearchHealthCheck(IOpenSearchClient client) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var nodes = client.ConnectionSettings.ConnectionPool.Nodes.ToArray();
            var pingResults = await PingNodesAsync(nodes, cancellationToken);
            var availableCount = pingResults.Count(available => available);

            if (availableCount == 0)
                return HealthCheckResult.Unhealthy("OpenSearch connection is not established");

            if (availableCount == pingResults.Length)
                return HealthCheckResult.Healthy("OpenSearch connection established");

            return HealthCheckResult.Degraded(
                $"OpenSearch connection degraded: {availableCount}/{pingResults.Length}");
        }

        async Task<bool[]> PingNodesAsync(Node[] nodes, CancellationToken cancellationToken)
        {
            var pingTasks = nodes.Select(node => PingNodeAsync(node, cancellationToken));
            var pingResults = await Task.WhenAll(pingTasks);
            return pingResults;
        }

        async Task<bool> PingNodeAsync(Node node, CancellationToken cancellationToken)
        {
            try
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

                var response = await client.PingAsync(request, cancellationToken);
                return response.IsValid;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }
    }
}
