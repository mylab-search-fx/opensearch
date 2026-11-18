using Microsoft.Extensions.DependencyInjection;
using MyLab.OpenSearch.Tools;
using OpenSearch.Client;
using OpenSearch.Net;
using Xunit.Abstractions;

namespace UnitTests.Tools
{
    public class OpenSearchClientFactoryBehavior(ITestOutputHelper output)
    {
        [Fact]
        public void ShouldCreateClientForSingleHost()
        {
            // Arrange
            var services = TestTools.CreateServices(output);

            services.ConfigureOpenSearch(options =>
                options.Host = "http://opensearch.invalid:9200");

            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var client = OpenSearchClientFactory.Create(serviceProvider);

            var pool = client.ConnectionSettings.ConnectionPool;
            var nodeUris = TestTools.GetNodeUris(pool);

            // Assert
            Assert.IsType<OpenSearchClient>(client);
            Assert.IsType<SingleNodeConnectionPool>(pool);
            Assert.Equal([new Uri("http://opensearch.invalid:9200")], nodeUris);
        }

        [Fact]
        public void ShouldCreateClientForHostPool()
        {
            // Arrange
            var services = TestTools.CreateServices(output);

            services.ConfigureOpenSearch(options =>
                options.HostPool =
                [
                    "http://node-a.invalid:9200",
                    "http://node-b.invalid:9200"
                ]);

            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var client = OpenSearchClientFactory.Create(serviceProvider);

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
        public void ShouldThrowWhenOpenSearchIsNotConfigured()
        {
            // Arrange
            var services = TestTools.CreateServices(output);
            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var exception = Record.Exception(() => OpenSearchClientFactory.Create(serviceProvider));

            // Assert
            var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("OpenSearch connection options is not defined", invalidOperation.Message);
        }

        [Fact]
        public void ShouldThrowWhenConnectionOptionsAreMissing()
        {
            // Arrange
            var services = TestTools.CreateServices(output);
            services.ConfigureOpenSearch(_ => { });
            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var exception = Record.Exception(() => OpenSearchClientFactory.Create(serviceProvider));

            // Assert
            var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("OpenSearch connection options is not defined", invalidOperation.Message);
        }
    }
}
