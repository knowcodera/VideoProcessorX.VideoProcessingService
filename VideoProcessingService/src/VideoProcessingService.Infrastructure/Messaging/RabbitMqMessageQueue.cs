using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VideoProcessingService.Application.Interfaces;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqMessageQueue : IMessageQueue
    {
        private readonly string _hostName;
        private readonly int _port;

        public RabbitMqMessageQueue(string hostName, int port = 5672)
        {
            _hostName = hostName;
            _port = port;
        }

        public async Task PublishAsync(string queueName, object message)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _hostName,
                Port = _port,
                DispatchConsumersAsync = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.BasicPublish(
               exchange: "video_exchange",
               routingKey: queueName,
               basicProperties: null,
               body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));

            await Task.CompletedTask;
        }
    }
}
