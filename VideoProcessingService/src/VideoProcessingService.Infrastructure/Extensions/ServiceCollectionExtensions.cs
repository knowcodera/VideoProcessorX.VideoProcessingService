using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using VideoProcessingService.Infrastructure.Configuration;

namespace VideoProcessingService.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            var factory = RabbitMqConfig.CreateConnectionFactory(configuration);
            var connection = factory.CreateConnection();

            services.AddSingleton<IConnection>(connection);
            services.AddSingleton<IModel>(provider =>
            {
                var channel = provider.GetRequiredService<IConnection>().CreateModel();
                RabbitMqInfrastructureConfig.ConfigureExchangesAndQueues(channel);
                return channel;
            });

            return services;
        }
    }
}
