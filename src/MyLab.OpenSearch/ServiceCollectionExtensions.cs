using Microsoft.Extensions.Configuration;
using MyLab.OpenSearch.Options;
using MyLab.OpenSearch.Tools;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130
{
    public static class ServiceCollectionExtensions
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddOpenSearch()
            {
                return services.AddSingleton(OpenSearchClientFactory.Create);
            }

            public IServiceCollection ConfigureOpenSearch(IConfiguration config,
                string sectionName = OpenSearchOptions.SectionName)
            {
                if (services == null) throw new ArgumentNullException(nameof(services));
                if (config == null) throw new ArgumentNullException(nameof(config));
                if (sectionName == null) throw new ArgumentNullException(nameof(sectionName));

                return services.Configure<OpenSearchOptions>(config.GetSection(sectionName));
            }

            public IServiceCollection ConfigureOpenSearch(Action<OpenSearchOptions> configure)
            {
                return services.Configure(configure);
            }
        }
    }
}
