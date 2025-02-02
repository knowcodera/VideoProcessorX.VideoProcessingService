using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Infrastructure.Data;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class VideoProcessingWorker : BackgroundService
    {
        private readonly ILogger<VideoProcessingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly int _maxDegreeOfParallelism;
        private readonly string _queueName = "video.process";
        private readonly string _deadLetterExchange = "dlx.video.process";
        private readonly string _deadLetterQueue = "video.process.dead";

        public VideoProcessingWorker(
            ILogger<VideoProcessingWorker> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            // Configuração de paralelismo
            _maxDegreeOfParallelism = Environment.ProcessorCount * 2;

            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                Port = int.Parse(configuration["RabbitMQ:Port"]),
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"],
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            ConfigureRabbitMQInfrastructure();

            // Configura QoS para controle de paralelismo
            _channel.BasicQos(0, (ushort)_maxDegreeOfParallelism, false);
        }

        private void ConfigureRabbitMQInfrastructure()
        {
            // Dead Letter Exchange
            _channel.ExchangeDeclare(_deadLetterExchange, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(_deadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.QueueBind(_deadLetterQueue, _deadLetterExchange, _deadLetterQueue);

            // Fila principal com DLX
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _deadLetterExchange },
                { "x-dead-letter-routing-key", _deadLetterQueue }
            };

            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
                _logger.LogInformation("Processamento de vídeos está sendo interrompido..."));

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var videoService = scope.ServiceProvider.GetRequiredService<IVideoService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<VideoProcessingWorker>>();

                try
                {
                    var body = ea.Body.ToArray();
                    var message = JsonSerializer.Deserialize<VideoProcessMessage>(body);

                    _logger.LogInformation($"Iniciando processamento do vídeo {message.VideoId} (DeliveryTag: {ea.DeliveryTag})");

                    var video = await dbContext.Videos
                                   .Include(v => v.User)
                                   .FirstOrDefaultAsync(v => v.Id == message.VideoId);

                    if (video?.User == null)
                    {
                        _logger.LogWarning($"Usuário não encontrado para o vídeo {message.VideoId}");
                        return;
                    }

                    await _messageQueue.PublishAsync("notification.events", new
                    {
                        Email = video.User.Email,
                        Subject = "Seu vídeo foi processado",
                        Body = $"Download disponível em: http://web:8080/api/videos/download/{video.Id}"
                    });

                    if (video == null || video.Status != "PENDING")
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    video.Status = "PROCESSING";
                    await dbContext.SaveChangesAsync();
                   
                    var zipPath = await videoService.GenerateFramesZipAsync(video.FilePath, video.Id);

                    video.ZipPath = zipPath;
                    video.Status = "COMPLETED";
                    video.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();

                    var user = await dbContext.Users.FindAsync(video.UserId);
                    if (user == null)
                    {
                        logger.LogWarning($"Usuário não encontrado para o vídeo {video.Id}");
                        return;
                    }

                    await messageQueue.PublishAsync("notification.events", new
                    {
                        Email = user.Email,
                        Subject = "Seu vídeo foi processado com sucesso",
                        Body = $"Seu vídeo {video.OriginalFileName} está pronto! " +
                     $"Download do arquivo ZIP: http://web:8080/api/videos/download/{video.Id}"
                    });

                    _channel.BasicAck(ea.DeliveryTag, false);
                    //_logger.LogInformation($"Vídeo {message.VideoId} processado com sucesso");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao processar vídeo (DeliveryTag: {ea.DeliveryTag})");

                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation($"Worker iniciado. Processando até {_maxDegreeOfParallelism} vídeos simultaneamente");

            return Task.CompletedTask;
        }


        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            _logger.LogInformation("Encerrando conexões com RabbitMQ...");
            _channel?.Close();
            _connection?.Close();

            _logger.LogInformation("Conexões com RabbitMQ encerradas");
        }
    }

    public record VideoProcessMessage(int VideoId);
}
