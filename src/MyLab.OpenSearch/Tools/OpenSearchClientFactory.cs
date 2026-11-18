using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.OpenSearch.Options;
using OpenSearch.Client;

namespace MyLab.OpenSearch.Tools
{
    static class OpenSearchClientFactory
    {
        public static IOpenSearchClient Create(IServiceProvider serviceProvider)
        {
            var opts = serviceProvider.GetRequiredService<IOptions<OpenSearchOptions>>();
            var logger = serviceProvider.GetService<ILogger>();
            var log = logger?.Dsl();

            try
            {
                var pool = ConnectionPoolFactory.Create(opts.Value);
                var settings = new ConnectionSettings(pool);
                return new OpenSearchClient(settings);
            }
            catch (Exception e)
            {
                log?.Error(e).Write();
            }
        }
    }
}
