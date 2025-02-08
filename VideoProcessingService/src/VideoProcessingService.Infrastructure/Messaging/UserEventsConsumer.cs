using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
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

        public UserEventsConsumer(IModel channel,
                                  ILogger<UserEventsConsumer> logger,
                                  IServiceScopeFactory scopeFactory)
        {
            _channel = channel;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            // Consumir da fila user.events
            _channel.BasicConsume("user.events", autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs e)
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                var body = e.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);

                _logger.LogInformation("UserEventsConsumer RECEIVED BODY: {messageJson}", messageJson);

                // Deserializar
                var message = JsonSerializer.Deserialize<UserCreatedMessage>(messageJson);

                _logger.LogInformation("Processing user event: {UserId} - {Email}", message.Id, message.Email);

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var existingUser = await dbContext.Users.FindAsync(message.Id);
                if (existingUser != null)
                {
                    existingUser.Email = message.Email;
                    existingUser.Username = message.Username;
                    dbContext.Users.Update(existingUser);
                }
                else
                {
                    // Importante: Se a PK "Id" for gerada pela AuthService e não for identity
                    // você precisa configurar ValueGeneratedNever() no seu OnModelCreating:
                    // modelBuilder.Entity<User>().Property(u => u.Id).ValueGeneratedNever();

                    var newUser = new User
                    {
                        Id = message.Id,
                        Email = message.Email,
                        Username = message.Username
                    };
                    await dbContext.Users.AddAsync(newUser);
                }

                await dbContext.SaveChangesAsync();

                // Ack da mensagem
                _channel.BasicAck(e.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user event");
                // NACK sem requeue para não cair em loop infinito
                _channel.BasicNack(e.DeliveryTag, multiple: false, requeue: false);
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
