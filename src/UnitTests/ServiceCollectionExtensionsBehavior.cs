using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;
using OpenSearch.Net;
using Xunit.Abstractions;

namespace UnitTests
{
    public class ServiceCollectionExtensionsBehavior(ITestOutputHelper output)
    {
        [Fact]
        public void ShouldResolveClientConfiguredByDelegate()
        {
            // Arrange
            var services = TestTools.CreateServices(output);

            services
                .AddOpenSearch()
                .ConfigureOpenSearch(options =>
                    options.Host = "http://opensearch.invalid:9200");

            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var client = serviceProvider.GetRequiredService<IOpenSearchClient>();

            var pool = client.ConnectionSettings.ConnectionPool;
            var nodeUris = TestTools.GetNodeUris(pool);

            // Assert
            Assert.IsType<SingleNodeConnectionPool>(pool);
            Assert.Equal([new Uri("http://opensearch.invalid:9200")], nodeUris);
        }

        [Fact]
        public void ShouldResolveClientConfiguredFromConfiguration()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenSearch:HostPool:0"] = "http://node-a.invalid:9200",
                    ["OpenSearch:HostPool:1"] = "http://node-b.invalid:9200"
                })
                .Build();

            var services = TestTools.CreateServices(output);

            services
                .AddOpenSearch()
                .ConfigureOpenSearch(config);

            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var client = serviceProvider.GetRequiredService<IOpenSearchClient>();

            var pool = client.ConnectionSettings.ConnectionPool;
            var nodeUris = TestTools.GetNodeUris(pool);

            // Assert
            Assert.IsType<StaticConnectionPool>(pool);
            Assert.Equal(
                [
                    new Uri("http://node-a.invalid:9200"),
                    new Uri("http://node-b.invalid:9200")
                ],
                nodeUris);
        }

        [Fact]
        public void ShouldResolveClientConfiguredFromNamedSection()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Search:Host"] = "http://search.invalid:9200"
                })
                .Build();

            var services = TestTools.CreateServices(output);

            services
                .AddOpenSearch()
                .ConfigureOpenSearch(config, "Search");

            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var client = serviceProvider.GetRequiredService<IOpenSearchClient>();

            var pool = client.ConnectionSettings.ConnectionPool;
            var nodeUris = TestTools.GetNodeUris(pool);

            // Assert
            Assert.IsType<SingleNodeConnectionPool>(pool);
            Assert.Equal([new Uri("http://search.invalid:9200")], nodeUris);
        }

        [Fact]
        public void ShouldThrowWhenServicesAreNull()
        {
            // Arrange
            IServiceCollection services = null!;
            var config = new ConfigurationBuilder().Build();

            // Act
            var exception = Record.Exception(() => services.ConfigureOpenSearch(config));

            // Assert
            var argumentException = Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("services", argumentException.ParamName);
        }

        [Fact]
        public void ShouldThrowWhenConfigurationIsNull()
        {
            // Arrange
            var services = TestTools.CreateServices(output);
            IConfiguration config = null!;

            // Act
            var exception = Record.Exception(() => services.ConfigureOpenSearch(config));

            // Assert
            var argumentException = Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("config", argumentException.ParamName);
        }

        [Fact]
        public void ShouldThrowWhenSectionNameIsNull()
        {
            // Arrange
            var services = TestTools.CreateServices(output);
            var config = new ConfigurationBuilder().Build();

            // Act
            var exception = Record.Exception(() => services.ConfigureOpenSearch(config, null!));

            // Assert
            var argumentException = Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("sectionName", argumentException.ParamName);
        }
    }
}
