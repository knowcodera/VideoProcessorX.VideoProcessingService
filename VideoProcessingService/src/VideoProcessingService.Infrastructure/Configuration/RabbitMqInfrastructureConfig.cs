using RabbitMQ.Client;

namespace VideoProcessingService.Infrastructure.Configuration
{
    public static class RabbitMqInfrastructureConfig
    {
        public static void ConfigureExchangesAndQueues(IModel channel)
        {
            channel.ExchangeDeclare("video_exchange", ExchangeType.Direct, durable: true);
          
            channel.ExchangeDeclare("dlx.video.process", ExchangeType.Direct, durable: true);
            
            var mainQueueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx.video.process" },
                { "x-message-ttl", 30000 } 
            };

            channel.QueueDeclare("video.process", durable: true, exclusive: false, autoDelete: false, arguments: mainQueueArgs);
            channel.QueueBind("video.process", "video_exchange", "video.process");

            channel.QueueDeclare("dead_letter.video.process", durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind("dead_letter.video.process", "dlx.video.process", "");

            channel.ExchangeDeclare("user_exchange", ExchangeType.Direct, durable: true);
            channel.QueueDeclare("user.events", durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind("user.events", "user_exchange", "user.created");
        }
    }
}
