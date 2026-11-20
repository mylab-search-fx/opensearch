using Microsoft.Extensions.DependencyInjection;
using MyLab.Log;
using MyLab.Log.XUnit;
using MyLab.OpenSearch.Options;
using Xunit.Abstractions;

namespace IntegrationTests
{
    static class TestTools
    {
        public static readonly OpenSearchBasicAuthentication AdminBasicAuth = new()
        {
            User = "admin",
            Password = ",w^&i!.m6A"
        };

        public static Action<OpenSearchOptions> ConfigureConnectedHost => options =>
        {
            options.Host = "https://localhost:19200";
            
        };

        public static Action<OpenSearchOptions> ConfigureDisconnectedHost => options =>
        {
            options.Host = "https://localhost:19201";
        };

        public static Action<OpenSearchOptions> ConfigureMixedHostPool => options =>
        {
            options.HostPool =
            [
                "https://localhost:19200",
                "https://localhost:19201"
            ];
        };

        public static ServiceProvider CreateApp(ITestOutputHelper? output, Action<OpenSearchOptions> configureOpenSearch)
        {
            var services = new ServiceCollection();

            services
                .AddLogging(c =>
                {
                    c.AddMyLabConsole();
                    if (output != null)
                        c.AddXUnit(output);
                })
                .AddOpenSearch()
                .ConfigureOpenSearch(configureOpenSearch)
                .ConfigureOpenSearch(o => o.IgnoreServerSslCertificate = true);

            services
                .AddHealthChecks()
                .AddOpenSearch();

            return services.BuildServiceProvider();
        }
    }
}
