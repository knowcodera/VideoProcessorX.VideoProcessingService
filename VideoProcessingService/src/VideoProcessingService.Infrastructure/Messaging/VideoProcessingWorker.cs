using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VideoProcessingService.Application.DTOs;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Infrastructure.Configuration;
using VideoProcessingService.Infrastructure.Data;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class VideoProcessingWorker : BackgroundService
    {
        private readonly IModel _channel;
        private readonly ILogger<VideoProcessingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ResiliencePolicyConfig _resilienceConfig;

        public VideoProcessingWorker(

            ILogger<VideoProcessingWorker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<ResiliencePolicyConfig> resilienceConfig,
            IModel channel)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _resilienceConfig = resilienceConfig.Value;
            _channel = channel;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)(Environment.ProcessorCount * 2), global: false);

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
            Video? video = null;
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

                    video = await dbContext.Videos
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

                    var zipPath = await videoService.GenerateFramesZipAsync(video.FilePath, video.Id);

                    video.Status = "COMPLETED";
                    video.ZipPath = zipPath;
                    video.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();


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

                if (video?.User != null && !string.IsNullOrEmpty(video.User.Email))
                {
                    await messageQueue.PublishAsync("notification.events", new NotificationMessageDto
                    {
                        Email = video.User.Email,
                        Subject = "Erro no processamento do vídeo",
                        Body = $"O processamento do vídeo '{video.OriginalFileName}' falhou. Detalhes: {ex.Message}",
                        AttachmentPath = null,
                        IsProcessingUpdate = false
                    });
                }
                else
                {
                    _logger.LogWarning("Não foi possível notificar o usuário sobre o erro. Video ou usuário não encontrado.");
                }

                channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }
    }

    public record VideoProcessMessage(int VideoId);
}
