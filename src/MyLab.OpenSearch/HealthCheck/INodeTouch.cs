namespace MyLab.OpenSearch.HealthCheck;

interface INodeTouch
{
    Task<TouchedNode> TouchAsync(CancellationToken ct);
}