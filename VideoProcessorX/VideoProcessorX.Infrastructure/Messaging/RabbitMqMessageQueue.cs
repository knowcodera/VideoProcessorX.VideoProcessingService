using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VideoProcessingService.Application.Interfaces;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqMessageQueue : IMessageQueue
    {
        private readonly string _hostName;

        public RabbitMqMessageQueue(string hostName)
        {
            _hostName = hostName;
        }

        public async Task PublishAsync(string queueName, object message)
        {
            // Cria conexão e canal em cada publicação (funciona bem para volume moderado)
            var factory = new ConnectionFactory()
            {
                HostName = _hostName,
                Port = 5672 // Porta padrão do RabbitMQ
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Declara a fila (importante para ter certeza que a fila existe)
            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Serializa o objeto em JSON
            var messageBody = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            // Publica a mensagem
            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: null,
                body: body);

            await Task.CompletedTask;
        }
    }
}
