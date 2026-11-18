using MyLab.OpenSearch.Options;
using OpenSearch.Net;

namespace MyLab.OpenSearch.Tools
{
    static class ConnectionPoolFactory
    {
        public static IConnectionPool Create(OpenSearchOptions options)
        {
            if (options == null) 
                throw new ArgumentNullException(nameof(options));
            
            if (options.Host != null)
                return new SingleNodeConnectionPool(new Uri(options.Host));

            if (options.HostPool is { Length: > 0 })
                return new StaticConnectionPool(options.HostPool.Select(h => new Uri(h)));

            throw new InvalidOperationException("OpenSearch connection options is not defined");
        }
    }
}
