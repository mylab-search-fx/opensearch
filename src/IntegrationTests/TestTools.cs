using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.Log;
using MyLab.Log.XUnit;
using MyLab.OpenSearch.HealthCheck;
using MyLab.OpenSearch.Options;
using Xunit.Abstractions;

namespace IntegrationTests
{
    static class TestTools
    {
        public static Action<OpenSearchOptions> ConfigureConnectedHost => options =>
        {
            options.Host = "http://localhost:19200";
        };

        public static Action<OpenSearchOptions> ConfigureDisconnectedHost => options =>
        {
            options.Host = "http://localhost:19201";
        };

        public static Action<OpenSearchOptions> ConfigureMixedHostPool => options =>
        {
            options.HostPool =
            [
                "http://localhost:19200",
                "http://localhost:19201"
            ];
        };

        public static ServiceProvider CreateApp(ITestOutputHelper output, Action<OpenSearchOptions> configureOpenSearch)
        {
            var services = new ServiceCollection();

            services
                .AddLogging(c => c
                    .AddMyLabConsole()
                    .AddXUnit(output)
                )
                .AddOpenSearch()
                .ConfigureOpenSearch(configureOpenSearch);

            services
                .AddHealthChecks()
                .AddOpenSearch();

            return services.BuildServiceProvider();
        }
    }
}
