using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VideoProcessingService.Application.Interfaces;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqMessageQueue : IMessageQueue
    {
   
        private readonly IConfiguration _configuration;

        public RabbitMqMessageQueue(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task PublishAsync(string queueName, object message)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"],
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(15)
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
