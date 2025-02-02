using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class VideoProcessingWorker : BackgroundService
    {
        private readonly ILogger<VideoProcessingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IModel _channel;

        // Configurações RabbitMQ
        private const string MainExchange = "video_exchange";
        private const string MainQueue = "video.process";
        private const string DlExchangeName = "dlx.video.process";
        private const string DlQueueName = "dead_letter.video.process";
        private const string NotificationQueue = "notification.events";
        private const int MaxConnectionRetries = 5;
        private const int InitialRetryDelayMs = 1000;

        public VideoProcessingWorker(
            ILogger<VideoProcessingWorker> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeRabbitMQWithRetry(stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            _channel.BasicConsume(
                queue: MainQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Worker de processamento de vídeo iniciado");
        }

        private async Task InitializeRabbitMQWithRetry(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                Port = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"],
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(15)
            };

            for (var attempt = 1; attempt <= MaxConnectionRetries; attempt++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    ConfigureRabbitInfrastructure();
                    _logger.LogInformation("Conexão com RabbitMQ estabelecida com sucesso");
                    return;
                }
                catch (BrokerUnreachableException ex)
                {
                    _logger.LogWarning(ex, "Falha na conexão com RabbitMQ. Tentativa {Attempt}/{MaxRetries}",
                        attempt, MaxConnectionRetries);

                    if (attempt == MaxConnectionRetries)
                    {
                        _logger.LogError("Número máximo de tentativas de conexão excedido");
                        throw;
                    }

                    await Task.Delay(InitialRetryDelayMs * attempt, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Erro crítico na inicialização do RabbitMQ");
                    throw;
                }
            }
        }

        private void ConfigureRabbitInfrastructure()
        {
            try
            {
                // 1. Configurar Exchanges
                SafeExchangeDeclare(MainExchange, ExchangeType.Direct);
                SafeExchangeDeclare(DlExchangeName, ExchangeType.Direct);

                // 2. Configurar Filas
                var mainQueueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", DlExchangeName },
                    { "x-message-ttl", 30000 }
                };

                SafeQueueDeclare(DlQueueName);
                SafeQueueDeclare(MainQueue, mainQueueArgs);
                SafeQueueDeclare(NotificationQueue);

                // 3. Configurar Bindings
                _channel.QueueBind(MainQueue, MainExchange, "video.process");
                _channel.QueueBind(DlQueueName, DlExchangeName, "");
                _channel.QueueBind(NotificationQueue, MainExchange, "notification");

                // 4. Configurar QoS
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation("Infraestrutura RabbitMQ configurada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Falha na configuração da infraestrutura RabbitMQ");
                throw;
            }
        }

        private void SafeExchangeDeclare(string exchangeName, string exchangeType)
        {
            try
            {
                _channel.ExchangeDeclarePassive(exchangeName);
                _logger.LogInformation("Exchange {ExchangeName} já existe", exchangeName);
            }
            catch (OperationInterruptedException ex) when (ex.IsNotFound())
            {
                try
                {
                    _channel.ExchangeDeclare(
                        exchange: exchangeName,
                        type: exchangeType,
                        durable: true,
                        autoDelete: false,
                        arguments: null);

                    _logger.LogInformation("Exchange {ExchangeName} criado com tipo {Type}",
                        exchangeName, exchangeType);
                }
                catch (OperationInterruptedException createEx) when (createEx.IsConfigurationMismatch())
                {
                    _logger.LogWarning("Exchange {ExchangeName} com configuração conflitante. Recriando...", exchangeName);
                    _channel.ExchangeDelete(exchangeName);
                    _channel.ExchangeDeclare(exchangeName, exchangeType, durable: true);
                    _logger.LogInformation("Exchange {ExchangeName} recriado com sucesso", exchangeName);
                }
            }
        }

        private void SafeQueueDeclare(string queueName, IDictionary<string, object> arguments = null)
        {
            try
            {
                _channel.QueueDeclarePassive(queueName);
                _logger.LogInformation("Fila {QueueName} já existe", queueName);
            }
            catch (OperationInterruptedException ex) when (ex.IsNotFound())
            {
                try
                {
                    _channel.QueueDeclare(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: arguments);

                    _logger.LogInformation("Fila {QueueName} criada com sucesso", queueName);
                }
                catch (OperationInterruptedException createEx) when (createEx.IsConfigurationMismatch())
                {
                    _logger.LogWarning("Fila {QueueName} com argumentos conflitantes. Recriando...", queueName);
                    _channel.QueueDelete(queueName);
                    _channel.QueueDeclare(queueName, true, false, false, arguments);
                    _logger.LogInformation("Fila {QueueName} recriada com novos argumentos", queueName);
                }
            }
        }

        private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<AppDbContext>();
            var videoService = services.GetRequiredService<IVideoService>();
            var messageQueue = services.GetRequiredService<IMessageQueue>();

            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<VideoProcessMessage>(body);

                _logger.LogInformation("Processando vídeo ID: {VideoId}", message.VideoId);

                var video = await dbContext.Videos
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.Id == message.VideoId);

                if (video == null || video.Status != "PENDING")
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                video.Status = "PROCESSING";
                await dbContext.SaveChangesAsync();

                var zipPath = await videoService.GenerateFramesZipAsync(video.FilePath, video.Id);

                video.Status = "COMPLETED";
                video.ZipPath = zipPath;
                video.ProcessedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                if (video.User != null)
                {
                    await messageQueue.PublishAsync("notification", new
                    {
                        Email = video.User.Email,
                        Subject = "Seu vídeo está pronto!",
                        Body = $"Download disponível: {GenerateDownloadLink(video.Id)}"
                    });
                }

                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Vídeo ID {VideoId} processado com sucesso", message.VideoId);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Erro de desserialização JSON");
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento do vídeo");
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }

        private string GenerateDownloadLink(int videoId)
        {
            return $"{_configuration["BaseUrl"]}/api/videos/download/{videoId}";
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _logger.LogInformation("Conexões RabbitMQ fechadas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao liberar recursos RabbitMQ");
            }
            finally
            {
                base.Dispose();
            }
        }
    }

    public static class RabbitMqExtensions
    {
        public static bool IsNotFound(this OperationInterruptedException ex)
        {
            return ex.Message.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsConfigurationMismatch(this OperationInterruptedException ex)
        {
            return ex.Message.Contains("inequivalent arg", StringComparison.OrdinalIgnoreCase);
        }
    }

    public record VideoProcessMessage(int VideoId);
}