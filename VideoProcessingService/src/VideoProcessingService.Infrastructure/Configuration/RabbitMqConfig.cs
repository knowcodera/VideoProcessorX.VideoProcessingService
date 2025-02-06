using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace VideoProcessingService.Infrastructure.Configuration
{
    public static class RabbitMqConfig
    {
        public static ConnectionFactory CreateConnectionFactory(IConfiguration configuration)
        {
            return new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"],
                Port = configuration.GetValue<int>("RabbitMQ:Port", 5672),
                UserName = configuration["RabbitMQ:Username"],
                Password = configuration["RabbitMQ:Password"],
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(15),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };
        }
    }
}
