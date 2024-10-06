using Colinapp.Services.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colinapp.Test
{
    public class RabbitMQServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<RabbitMQService>> _mockLogger;
        private readonly RabbitMQService _service;
        private readonly Mock<IConnectionFactory> _mockFactory;
        private readonly Mock<IConnection> _mockConnection;
        private readonly Mock<IModel> _mockChannel;

        public RabbitMQServiceTests()
        {
            // Initialize mocks
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<RabbitMQService>>();
            _mockFactory = new Mock<IConnectionFactory>();
            _mockConnection = new Mock<IConnection>();
            _mockChannel = new Mock<IModel>();

            // Setup mock configuration values
            _mockConfiguration.Setup(config => config["RabbitMQ:HostName"]).Returns("172.28.112.163");
            _mockConfiguration.Setup(config => config["RabbitMQ:UserName"]).Returns("admin");
            _mockConfiguration.Setup(config => config["RabbitMQ:Password"]).Returns("admin");

            // Setup connection and channel behavior
            _mockFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(c => c.CreateModel()).Returns(_mockChannel.Object);

            // Initialize RabbitMQService with mocks
            _service = new RabbitMQService(_mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public void CreateConnection_ShouldAddConnection_WhenNewConnectionIsCreated()
        {
            // Act
            _service.CreateConnection("testConnection");

            _service.SendMessage("testConnection", "fulfillSendAuthorityQueue", "test");

            _service.ReceiveMessage("testConnection", "punchingCardRecordQueue", message => { Console.WriteLine(message); });

            // Assert that the connection is added to the _connections dictionary
        }

        /*[Fact]
        public void SendMessage_ShouldLogError_WhenConnectionDoesNotExist()
        {
            // Act
            _service.SendMessage("nonExistingConnection", "testQueue", "Hello, World!");

            // Verify that the logger logs an error when connection doesn't exist
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Connection nonExistingConnection not found.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));
        }

        [Fact]
        public void SendMessage_ShouldPublishMessage_WhenConnectionExists()
        {
            // Arrange
            _service.CreateConnection("testConnection");

            // Act
            _service.SendMessage("testConnection", "testQueue", "Hello, World!");

            // Verify that BasicPublish is called on the channel

        }

        [Fact]
        public void CloseConnection_ShouldCloseConnection_WhenConnectionExists()
        {
            // Arrange
            _service.CreateConnection("testConnection");

            // Act
            _service.CloseConnection("testConnection");

            // Verify that the channel and connection are closed
            _mockChannel.Verify(channel => channel.Close(), Times.Once);
            _mockConnection.Verify(connection => connection.Close(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldCloseAllConnections()
        {
            // Arrange
            _service.CreateConnection("testConnection1");
            _service.CreateConnection("testConnection2");

            // Act
            _service.Dispose();

            // Verify that all channels and connections are closed
            _mockChannel.Verify(channel => channel.Close(), Times.Exactly(2));
            _mockConnection.Verify(connection => connection.Close(), Times.Exactly(2));
        }*/
    }
}
