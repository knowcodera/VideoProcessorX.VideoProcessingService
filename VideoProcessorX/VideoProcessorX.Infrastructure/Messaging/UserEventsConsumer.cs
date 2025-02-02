using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class UserEventsConsumer : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<UserCreatedMessage>(body);

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var user = new User { Id = message.Id, Email = message.Email };
                    dbContext.Users.Upsert(user).Run();
                    await dbContext.SaveChangesAsync();

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    // Tratar erro
                }
            };

            _channel.BasicConsume(queue: "user.events", autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }
    }
}
