using Microsoft.Extensions.DependencyInjection;
using MyLab.OpenSearch.Options;
using OpenSearch.Client;

namespace IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OpenSearchCollection : ICollectionFixture<GlobalTestFixture>
{
    public const string Name = "OpenSearch";
}

public sealed class GlobalTestFixture : IAsyncLifetime
{
    ServiceProvider? _app;

    public async Task InitializeAsync()
    {
        _app = TestTools.CreateApp(null, opt =>
        {
            TestTools.ConfigureConnectedHost(opt);
            opt.BasicAuth = new OpenSearchBasicAuthentication
            {
                User = TestTools.AdminBasicAuth.User,
                Password = TestTools.AdminBasicAuth.Password
            };
        });

        var client = _app.GetRequiredService<IOpenSearchClient>();

        var tuning = new OpenSearchTuning(client);

        await tuning.CreateMonitorUserAsync(TestTools.MonitorBasicAuth.User!, TestTools.MonitorBasicAuth.Password!);
        await tuning.CreateUserAsync(TestTools.NoRightsBasicAuth.User!, TestTools.NoRightsBasicAuth.Password!);
    }
    
    public Task DisposeAsync()
    {
        return _app?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }
}