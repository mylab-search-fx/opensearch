using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.Log;
using MyLab.Log.XUnit;
using OpenSearch.Net;
using Xunit.Abstractions;

namespace UnitTests
{
    static class TestTools
    {
        public static IServiceCollection CreateServices(ITestOutputHelper output)
        {
            var services = new ServiceCollection();

            services
                .AddLogging(c => c
                    .AddMyLabConsole()
                    .AddXUnit(output)
                );

            return services;
        }

        public static Uri[] GetNodeUris(IConnectionPool pool)
        {
            return pool.Nodes
                .Select(node => node.Uri)
                .OrderBy(uri => uri.AbsoluteUri, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
