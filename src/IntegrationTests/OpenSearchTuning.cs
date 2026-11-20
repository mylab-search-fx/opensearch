using OpenSearch.Client;
using OpenSearch.Net;

namespace IntegrationTests;

class OpenSearchTuning(IOpenSearchClient opensearch)
{
    const string HealthCheckRoleName = "cluster_health_check";

    public async Task CreateMonitorUserAsync(string username, string password)
    {
        await CreateHealthCheckRoleAsync(HealthCheckRoleName);
        await CreateUserAsync(username, password);
        await SetHealthCheckRoleAsync(username, HealthCheckRoleName);
    }

    async Task CreateHealthCheckRoleAsync(string roleName)
    {
        var res = await opensearch.LowLevel.Security.CreateRoleAsync<StringResponse>(
            roleName,
            """
            {
              "cluster_permissions": [
                "cluster:monitor/main"
              ]
            }
            """);

        CheckResult(res);
    }

    public async Task CreateUserAsync(string username, string password)
    {
        var res = await opensearch.LowLevel.Security.CreateUserAsync<StringResponse>(
            username,
            $$"""
            {
                "password": "{{password}}"
            }
            """
        );

        CheckResult(res);
    }

    async Task SetHealthCheckRoleAsync(string userName, string roleName)
    {
        var res = await opensearch.LowLevel.Security.CreateRoleMappingAsync<StringResponse>(
                roleName, 
            $$"""
            {
              "users": [
                  "{{userName}}"
                ]
            }
            """
        );

        CheckResult(res);
    }

    void CheckResult(IApiCallDetails callDetails)
    {
        if (!callDetails.Success)
            throw new InvalidOperationException("Request error");
    }
}