using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqListener : BackgroundService
    {
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqListener> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _queueName;
        private readonly string _exchangeName;
        private readonly string _routingKey;

        public RabbitMqListener(
            IModel channel,
            ILogger<RabbitMqListener> logger,
            IServiceScopeFactory scopeFactory,
            string queueName,
            string exchangeName = "",
            string routingKey = "")
        {
            _channel = channel;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _queueName = queueName;
            _exchangeName = exchangeName;
            _routingKey = routingKey;

            Initialize();
        }

        private void Initialize()
        {
            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: _routingKey);

            _channel.BasicQos(0, 1, false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
                _logger.LogInformation("Listener for {QueueName} is stopping", _queueName));

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            _channel.BasicConsume(_queueName, false, consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Processing message from {QueueName}", _queueName);

                // Implementar lógica específica do consumidor via DI
                var handler = serviceProvider.GetRequiredService<IMessageHandler>();
                await handler.HandleAsync(message);

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {QueueName}", _queueName);
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }
    }

    public interface IMessageHandler
    {
        Task HandleAsync(string message);
    }
}
