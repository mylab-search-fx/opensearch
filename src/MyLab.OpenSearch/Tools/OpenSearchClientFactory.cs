using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.OpenSearch.Options;
using OpenSearch.Client;
using OpenSearch.Net;

namespace MyLab.OpenSearch.Tools;

static class OpenSearchClientFactory
{
    public static IOpenSearchClient Create(IServiceProvider serviceProvider)
    {
        var opts = serviceProvider.GetRequiredService<IOptions<OpenSearchOptions>>();
        var logger = serviceProvider.GetService<ILogger>();
        var log = logger?.Dsl();

        try
        {
            var connectionPool = ConnectionPoolFactory.Create(opts.Value);
            var settings = CreateSettings(opts.Value, connectionPool);
            return new OpenSearchClient(settings);
        }
        catch (Exception e)
        {
            log?.Error(e).Write();
            throw;
        }
    }

    public static ConnectionSettings CreateSettings(OpenSearchOptions opts, IConnectionPool pool)
    {
        var s = new ConnectionSettings(pool);

        if (opts is { ApiKeyAuth: not null, BasicAuth: not null })
        {
            throw new InvalidOperationException("The ApiKeyAuth and BasicAuth are mutually exclusive");
        }
        
        if (opts.ApiKeyAuth != null)
        {
            var gHeaders = new NameValueCollection { { "Authorization", $"ApiKey {opts.ApiKeyAuth.Token}" } };

            s = s.GlobalHeaders(gHeaders);
        }

        if (opts.BasicAuth != null)
        {
            s = opts.BasicAuth.IsValid() ?
                s.BasicAuthentication(opts.BasicAuth.User, opts.BasicAuth.Password) 
                : throw new InvalidOperationException($"{nameof(opts.BasicAuth)} is invalid");
        }

        if (opts.IgnoreServerSslCertificate)
        {
            s = s.ServerCertificateValidationCallback((_, _, _, _) => true);
        }

        s = s.EnableDebugMode(cd =>
        {

        });

        return s;
    }
}