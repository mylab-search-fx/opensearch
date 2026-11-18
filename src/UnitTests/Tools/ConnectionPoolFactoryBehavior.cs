using MyLab.OpenSearch.Options;
using MyLab.OpenSearch.Tools;
using OpenSearch.Net;

namespace UnitTests.Tools
{
    public class ConnectionPoolFactoryBehavior
    {
        [Fact]
        public void ShouldThrowWhenOptionsAreNull()
        {
            // Arrange

            // Act
            var exception = Record.Exception(() => ConnectionPoolFactory.Create(null!));

            // Assert
            var argumentException = Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("options", argumentException.ParamName);
        }

        [Fact]
        public void ShouldCreateSingleNodePoolWhenHostIsSet()
        {
            // Arrange
            var options = new OpenSearchOptions
            {
                Host = "http://opensearch.invalid:9200"
            };

            // Act
            var pool = ConnectionPoolFactory.Create(options);

            var nodeUris = TestTools.GetNodeUris(pool);

            // Assert
            Assert.IsType<SingleNodeConnectionPool>(pool);
            Assert.Equal([new Uri("http://opensearch.invalid:9200")], nodeUris);
        }

        [Fact]
        public void ShouldCreateStaticPoolWhenHostPoolIsSet()
        {
            // Arrange
            var options = new OpenSearchOptions
            {
                HostPool =
                [
                    "http://node-a.invalid:9200",
                    "http://node-b.invalid:9200"
                ]
            };

            // Act
            var pool = ConnectionPoolFactory.Create(options);

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
        public void ShouldUseHostWhenHostPoolIsAlsoSet()
        {
            // Arrange
            var options = new OpenSearchOptions
            {
                Host = "http://single.invalid:9200",
                HostPool =
                [
                    "http://node-a.invalid:9200",
                    "http://node-b.invalid:9200"
                ]
            };

            // Act
            var pool = ConnectionPoolFactory.Create(options);

            var nodeUris = TestTools.GetNodeUris(pool);

            // Assert
            Assert.IsType<SingleNodeConnectionPool>(pool);
            Assert.Equal([new Uri("http://single.invalid:9200")], nodeUris);
        }

        [Fact]
        public void ShouldThrowWhenConnectionOptionsAreMissing()
        {
            // Arrange
            var options = new OpenSearchOptions();

            // Act
            var exception = Record.Exception(() => ConnectionPoolFactory.Create(options));

            // Assert
            var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("OpenSearch connection options is not defined", invalidOperation.Message);
        }

        [Fact]
        public void ShouldThrowWhenHostPoolIsEmpty()
        {
            // Arrange
            var options = new OpenSearchOptions
            {
                HostPool = []
            };

            // Act
            var exception = Record.Exception(() => ConnectionPoolFactory.Create(options));

            // Assert
            var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("OpenSearch connection options is not defined", invalidOperation.Message);
        }

        [Fact]
        public void ShouldThrowWhenHostIsNotUri()
        {
            // Arrange
            var options = new OpenSearchOptions
            {
                Host = "not-a-uri"
            };

            // Act
            var exception = Record.Exception(() => ConnectionPoolFactory.Create(options));

            // Assert
            Assert.IsType<UriFormatException>(exception);
        }
    }
}
