using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VideoProcessingService.Application.Interfaces;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqMessageQueue : IMessageQueue
    {
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqMessageQueue> _logger;

        public RabbitMqMessageQueue(
            IModel channel,
            ILogger<RabbitMqMessageQueue> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public async Task PublishAsync(string queueName, object message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published message to {QueueName}", queueName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to {QueueName}", queueName);
                throw;
            }
        }
    }
}
