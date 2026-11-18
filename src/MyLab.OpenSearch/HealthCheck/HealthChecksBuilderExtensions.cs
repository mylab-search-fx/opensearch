using MyLab.OpenSearch.HealthCheck;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

/// <summary>
/// Registers the OpenSearch health check.
/// </summary>
public static class HealthChecksBuilderExtensions
{
    /// <summary>
    /// Adds the OpenSearch health check named <c>opensearch</c>.
    /// </summary>
    /// <param name="builder">Health-check builder.</param>
    public static IHealthChecksBuilder AddOpenSearch(this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<OpenSearchHealthCheck>("opensearch");
    }
}