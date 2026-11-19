using Microsoft.Extensions.DependencyInjection;
using MyLab.OpenSearch.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Tokens;

namespace IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OpenSearchCollection : ICollectionFixture<GlobalTestFixture>
{
    public const string Name = "OpenSearch";
}

public sealed class GlobalTestFixture : IAsyncLifetime
{
    ServiceProvider? _app;

    public OpenSearchApiKeyAuthentication? ApiKey { get; private set; }

    public async Task InitializeAsync()
    {
        _app = TestTools.CreateApp(null, opt =>
        {
            TestTools.ConfigureConnectedHost(opt);
            opt.BasicAuth = new OpenSearchBasicAuthentication
            {
                User = "admin",
                Password = ",w^&i!.m6A"
            };
        });

        var client = _app.GetRequiredService<IOpenSearchClient>();

        var postResponse = await CreateTokenAsync(client);

        if (!postResponse.Success)
        {
            if (postResponse.OriginalException != null)
            {
                throw postResponse.OriginalException;
            }
            throw new InvalidOperationException(postResponse.DebugInformation);
        }

        var created = ExtractResponse(postResponse.Body);

        ApiKey = new OpenSearchApiKeyAuthentication
        {
            Id = created.Id,
            Token = created.Token
        };
    }

    static (string Id, string Token) ExtractResponse(string response)
    {
        var doc = JsonDocument.Parse(response);

        var fields = doc.RootElement.EnumerateObject().ToArray();
        var id = fields.FirstOrDefault(f => f.Name == "id");
        var token = fields.FirstOrDefault(f => f.Name == "token");

        if (id.Value.ValueKind == JsonValueKind.Null || token.Value.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException("Created ApiKey id and token not found");
        }

        return (Id: id.Value.ToString(), Token: token.Value.ToString());
    }

    static Task<StringResponse> CreateTokenAsync(IOpenSearchClient client)
    {
        string appName = "test-app-" + Guid.NewGuid();

        return client.LowLevel.DoRequestAsync<StringResponse>(
            OpenSearch.Net.HttpMethod.POST,
            "_plugins/_security/api/apitokens",
            CancellationToken.None,
            """
            {
              "name": "{app-name}",
              "duration_seconds": 2592000,
              "cluster_permissions": [ "cluster_monitor" ],
              "index_permissions": [
                {
                  "index_pattern": [ "*" ],
                  "allowed_actions": [ "crud" ]
                }
              ]
            }
            """.Replace("{app-name}", appName));
    }

    public Task DisposeAsync()
    {
        return _app?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }
}