using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colinapp.Services.RabbitMQ
{
    /// <summary>
    /// rabbitMQ 多连接服务模型
    /// </summary>
    public class RabbitMQService : IDisposable
    {
        /// <summary>
        /// 连接信息
        /// </summary>
        private readonly ConcurrentDictionary<string, (IConnection connection, IModel channel)> _connections;

        /// <summary>
        /// 日志文件
        /// </summary>
        private readonly ILogger<RabbitMQService> _logger;

        /// <summary>
        /// 配置文件
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置信息</param>
        /// <param name="logger">日志</param>
        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _connections = new ConcurrentDictionary<string, (IConnection, IModel)>();
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 创建一个新的连接和通道
        /// </summary>
        /// <param name="connectionName">连接的标识符</param>
        public void CreateConnection(string connectionName)
        {
            if (_connections.ContainsKey(connectionName))
            {
                _logger.LogWarning("Connection {ConnectionName} already exists.", connectionName);
                return;
            }

            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            _connections.TryAdd(connectionName, (connection, channel));
            _logger.LogInformation("Connection {ConnectionName} created successfully.", connectionName);
        }

        /// <summary>
        /// 向指定连接的队列发送消息
        /// </summary>
        /// <param name="connectionName">连接的标识符</param>
        /// <param name="queue">队列名</param>
        /// <param name="message">消息内容</param>
        public void SendMessage(string connectionName, string queue, string message)
        {
            if (!_connections.TryGetValue(connectionName, out var connectionData))
            {
                _logger.LogError("Connection {ConnectionName} not found.", connectionName);
                return;
            }

            var body = Encoding.UTF8.GetBytes(message);

            connectionData.channel.BasicPublish(exchange: "",
                                 routingKey: queue,
                                 basicProperties: null,
                                 body: body);

            _logger.LogInformation("Message sent to {Queue} on connection {ConnectionName}.", queue, connectionName);
        }

        /// <summary>
        /// 接收指定连接的队列消息
        /// </summary>
        /// <param name="connectionName">连接的标识符</param>
        /// <param name="queueName">队列名</param>
        /// <param name="handleMessage">消息处理逻辑</param>
        public void ReceiveMessage(string connectionName, string queueName, Action<string> handleMessage)
        {
            if (!_connections.TryGetValue(connectionName, out var connectionData))
            {
                _logger.LogError("Connection {ConnectionName} not found.", connectionName);
                return;
            }

            var consumer = new EventingBasicConsumer(connectionData.channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Message received: {Message} on connection {ConnectionName}.", message, connectionName);

                // 调用传入的处理逻辑
                handleMessage(message);

                // 手动确认消息已处理
                connectionData.channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            connectionData.channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Started receiving messages from {QueueName} on connection {ConnectionName}.", queueName, connectionName);
        }

        /// <summary>
        /// 关闭指定连接
        /// </summary>
        /// <param name="connectionName">连接的标识符</param>
        public void CloseConnection(string connectionName)
        {
            if (_connections.TryRemove(connectionName, out var connectionData))
            {
                connectionData.channel.Close();
                connectionData.connection.Close();
                _logger.LogInformation("Connection {ConnectionName} closed.", connectionName);
            }
            else
            {
                _logger.LogError("Connection {ConnectionName} not found for closing.", connectionName);
            }
        }

        /// <summary>
        /// 释放所有连接
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _connections)
            {
                kvp.Value.channel.Close();
                kvp.Value.connection.Close();
            }

            _connections.Clear();
            _logger.LogInformation("All connections closed and resources released.");
        }
    }
}
