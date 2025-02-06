using RabbitMQ.Client;

namespace VideoProcessingService.Infrastructure.Configuration
{
    public static class RabbitMqInfrastructureConfig
    {
        public static void ConfigureExchangesAndQueues(IModel channel)
        {
            try
            {
                channel.ExchangeDeclare("video_exchange", ExchangeType.Direct, durable: true);
                channel.ExchangeDeclare("dlx.video.process", ExchangeType.Direct, durable: true);

                var mainQueueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx.video.process" },
                { "x-message-ttl", 30000 }
            };

                channel.QueueDeclare("video.process", durable: true, exclusive: false, autoDelete: false, arguments: mainQueueArgs);
                channel.QueueDeclare("dead_letter.video.process", durable: true, exclusive: false, autoDelete: false);

                channel.QueueBind("video.process", "video_exchange", "video.process");
                channel.QueueBind("dead_letter.video.process", "dlx.video.process", "");
            }
            catch (Exception ex)
            {
                throw new RabbitMqConfigurationException("Failed to configure RabbitMQ infrastructure", ex);
            }
        }
    }

    public class RabbitMqConfigurationException : Exception
    {
        public RabbitMqConfigurationException(string message, Exception inner)
            : base(message, inner) { }
    }
}
