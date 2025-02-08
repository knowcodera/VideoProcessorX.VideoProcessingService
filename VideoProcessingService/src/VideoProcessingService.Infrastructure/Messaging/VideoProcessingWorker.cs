using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using VideoProcessingService.Application.DTOs;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Infrastructure.Configuration;
using VideoProcessingService.Infrastructure.Data;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class VideoProcessingWorker : BackgroundService
    {
        private readonly IModel _channel;
        private readonly ILogger<VideoProcessingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ResiliencePolicyConfig _resilienceConfig;

        public VideoProcessingWorker(

            ILogger<VideoProcessingWorker> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IOptions<ResiliencePolicyConfig> resilienceConfig,
            IModel channel)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _resilienceConfig = resilienceConfig.Value;
            _channel = channel;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            _channel.BasicConsume(
                queue: "video.process",
                autoAck: false,
                consumer: consumer
            );

            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;
            var channel = scope.ServiceProvider.GetRequiredService<IModel>();

            var dbContext = services.GetRequiredService<AppDbContext>();
            var videoService = services.GetRequiredService<IVideoService>();
            var messageQueue = services.GetRequiredService<IMessageQueue>();

            var policy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(
                    _resilienceConfig.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(
                        Math.Pow(_resilienceConfig.RetryBaseDelaySeconds, retryAttempt)),
                    onRetry: (ex, delay) =>
                        _logger.LogWarning(ex, "Retrying video processing after {Delay}s", delay.TotalSeconds));

            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    var body = ea.Body.ToArray();
                    var message = JsonSerializer.Deserialize<VideoProcessMessage>(body);

                    _logger.LogInformation("Processing video ID: {VideoId}", message.VideoId);

                    var video = await dbContext.Videos
                        .Include(v => v.User)
                        .FirstOrDefaultAsync(v => v.Id == message.VideoId);

                    if (video?.Status != "PENDING") return;

                    video.Status = "PROCESSING";
                    await dbContext.SaveChangesAsync();

                    if (video.User != null)
                    {
                        await messageQueue.PublishAsync("notification.events", new NotificationMessageDto
                        {
                            Email = video.User.Email,
                            Subject = "Processamento iniciado",
                            Body = $"Seu vídeo '{video.OriginalFileName}' está sendo processado.",
                            AttachmentPath = null,
                            IsProcessingUpdate = true
                        });
                    }

                    // Processar vídeo
                    var zipPath = await videoService.GenerateFramesZipAsync(video.FilePath, video.Id);

                    // Atualizar status para COMPLETED
                    video.Status = "COMPLETED";
                    video.ZipPath = zipPath;
                    video.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();

                    //// Enviar notificação
                    //if (video.User != null)
                    //{
                    //    await messageQueue.PublishAsync("notification.events", new NotificationMessageDto
                    //    {
                    //        Email = video.User.Email,
                    //        Subject = "Seu vídeo está pronto!",
                    //        Body = $"Download disponível: {GenerateDownloadLink(video.Id)}"
                    //    });
                    //}

                    if (video.User != null)
                    {
                        await messageQueue.PublishAsync("notification.events", new NotificationMessageDto
                        {
                            Email = video.User.Email,
                            Subject = "Seu vídeo está pronto!",
                            Body = $"Olá, em anexo está seu ZIP contendo os frames do vídeo '{video.OriginalFileName}'",
                            AttachmentPath = zipPath,
                            IsProcessingUpdate = false
                        });
                    }

                    channel.BasicAck(ea.DeliveryTag, false);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process video after {Retries} retries", _resilienceConfig.RetryCount);
                channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }

        private string GenerateDownloadLink(int videoId)
        {
            return $"{_configuration["BaseUrl"]}/api/videos/download/{videoId}";
        }
    }

    public record VideoProcessMessage(int VideoId);
}