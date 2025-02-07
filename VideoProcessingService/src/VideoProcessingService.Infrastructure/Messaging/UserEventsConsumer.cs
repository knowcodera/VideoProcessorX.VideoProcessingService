using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Infrastructure.Data;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class UserEventsConsumer : BackgroundService
    {
        private readonly IModel _channel;
        private readonly ILogger<UserEventsConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private const string QueueName = "user.events";

        public UserEventsConsumer(
            ILogger<UserEventsConsumer> logger,
            IServiceScopeFactory scopeFactory,
            IModel channel)
        {

            _logger = logger;
            _scopeFactory = scopeFactory;
            _channel = channel;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            _channel.BasicConsume(
                queue: "user.events",
                autoAck: false,
                consumer: consumer
            );

            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>(); 
            var channel = scope.ServiceProvider.GetRequiredService<IModel>();

            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<UserCreatedMessage>(body);

                _logger.LogInformation("Processing user event: {UserId}", message.Id);

                var existingUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == message.Id);

                if (existingUser != null)
                {
                    existingUser.Email = message.Email;
                    existingUser.Username = message.Username;
                    dbContext.Users.Update(existingUser);
                }
                else
                {
                    var newUser = new User
                    {
                        Id = message.Id,
                        Email = message.Email,
                        Username = message.Username
                    };
                    await dbContext.Users.AddAsync(newUser);
                }

                await dbContext.SaveChangesAsync();
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user event");
                channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }
    }

    public class UserCreatedMessage
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
    }
}
