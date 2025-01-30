using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using VideoProcessingService.Infrastructure.Data;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqListener
    {
        private readonly ILogger<RabbitMqListener> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;

        private readonly string _queueName = "video.uploaded";

        public RabbitMqListener(
            ILogger<RabbitMqListener> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public void Start()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = "localhost"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                _consumer = new EventingBasicConsumer(_channel);
                _consumer.Received += (model, ea) =>
                {
                    // Aqui dentro, criamos um "escopo" para cada mensagem:
                    using var scope = _scopeFactory.CreateScope();

                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        _logger.LogInformation($"[RabbitMqListener] Mensagem recebida: {message}");

                        // Recupera o DbContext ou o repositório a partir do escopo
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // Faça o que for necessário:
                        // dbContext.Videos.Add(...);
                        // dbContext.SaveChanges();

                        // Confirma que processou
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[RabbitMqListener] Erro ao processar a mensagem.");
                        // Em caso de erro, você pode dar BasicNack com requeue ou descartar
                        //_channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                // Inicia o consumo
                _channel.BasicConsume(
                    queue: _queueName,
                    autoAck: false, // false para controlar manualmente
                    consumer: _consumer
                );

                _logger.LogInformation("[RabbitMqListener] Iniciado. Escutando a fila...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RabbitMqListener] Falha ao iniciar.");
            }
        }

        public void Stop()
        {
            _logger.LogInformation("[RabbitMqListener] Parando...");

            _channel?.Close();
            _connection?.Close();

            _logger.LogInformation("[RabbitMqListener] Parado.");
        }
    }
}
