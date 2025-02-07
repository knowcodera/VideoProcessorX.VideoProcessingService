using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Infrastructure.Messaging;

namespace VideoProcessingService.Infrastructure.Extensions
{
    public static class RabbitMqRegistrationExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            // 1) Singleton da conexão
            services.AddSingleton<IConnection>(sp =>
            {
                var host = configuration["RabbitMQ:Host"];
                var port = configuration.GetValue<int>("RabbitMQ:Port", 5672);

                var factory = new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = configuration["RabbitMQ:Username"],
                    Password = configuration["RabbitMQ:Password"],
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(15),
                    RequestedHeartbeat = TimeSpan.FromSeconds(60)
                };

                return factory.CreateConnection();
            });

            // 2) Singleton do canal (IModel)
            services.AddSingleton<IModel>(sp =>
            {
                var connection = sp.GetRequiredService<IConnection>();
                var channel = connection.CreateModel();

                // Declara as exchanges e filas necessárias
                // Exemplo: video_exchange e fila video.process
                channel.ExchangeDeclare(
                    exchange: "video_exchange",
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false
                );

                channel.ExchangeDeclare(
                    exchange: "dlx.video.process",
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false
                );

                var mainQueueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx.video.process" },
                { "x-message-ttl", 30000 }  // Exemplo de TTL de 30 seg
            };

                channel.QueueDeclare(
                    queue: "video.process",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: mainQueueArgs
                );

                channel.QueueDeclare(
                    queue: "dead_letter.video.process",
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                channel.QueueBind("video.process", "video_exchange", "video.process");
                channel.QueueBind("dead_letter.video.process", "dlx.video.process", "");

                // Se quiser consumir user.created, declare e bind "user_exchange"
                // (caso esse exchange não seja declarado por outro serviço)
                channel.ExchangeDeclare("user_exchange", ExchangeType.Direct, true, false);
                channel.QueueDeclare("user.events", durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind("user.events", "user_exchange", "user.created");

                return channel;
            });

            // 3) Registrar a classe que publica mensagem
            //    Se quisermos que os controllers/serviços usem IMessageQueue, basta SingleTon também
            services.AddSingleton<IMessageQueue, RabbitMqMessageQueue>();

            return services;
        }
    }

}
