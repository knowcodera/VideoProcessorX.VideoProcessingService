using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VideoProcessingService.Application.Interfaces;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqMessageQueue : IMessageQueue, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqMessageQueue> _logger;
        private readonly RabbitMQSettings _settings;
        private bool _disposed;

        public RabbitMqMessageQueue(
            IOptions<RabbitMQSettings> options,
            ILogger<RabbitMqMessageQueue> logger)
        {
            _settings = options.Value;
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(30)
            };

            if (_settings.UseSsl)
            {
                factory.Ssl = new SslOption(
                    enabled: true,
                    serverName: _settings.HostName)
                {
                    AcceptablePolicyErrors = System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch |
                                           System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors
                };
            }

            _connection = factory.CreateConnection("VideoProcessorX-Producer");
            _channel = _connection.CreateModel();

            _logger.LogInformation("RabbitMQ connection established");
        }

        public async Task PublishAsync(string queueName, object message)
        {
            try
            {
                EnsureNotDisposed();

                // Declaração robusta da fila
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,    // Fila persistente
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                        {"x-queue-type", "quorum"} // Tipo de fila mais resiliente
                    });

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;      // Mensagem persistente
                properties.DeliveryMode = 2;       // Persistência em disco
                properties.Headers = new Dictionary<string, object>
                {
                    {"source", "VideoProcessorX"},
                    {"version", "1.0"}
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await Task.Run(() =>
                {
                    _channel.BasicPublish(
                        exchange: string.Empty,
                        routingKey: queueName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                });

                _logger.LogDebug("Message published to {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to {QueueName}", queueName);
                throw new RabbitMqException("Failed to publish message", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _channel?.Close();
                _connection?.Close();

                _channel?.Dispose();
                _connection?.Dispose();

                _logger.LogInformation("RabbitMQ connection closed");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error disposing RabbitMQ resources");
            }
            finally
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }

    public class RabbitMQSettings
    {
        public string HostName { get; set; }
        public int Port { get; set; } = 5672;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; } = "/";
        public bool UseSsl { get; set; } = false;
    }

    public class RabbitMqException : Exception
    {
        public RabbitMqException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
