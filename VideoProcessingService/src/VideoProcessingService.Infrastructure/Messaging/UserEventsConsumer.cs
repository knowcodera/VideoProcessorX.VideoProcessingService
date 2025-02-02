using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Infrastructure.Data;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class UserEventsConsumer : BackgroundService
    {
        private readonly ILogger<UserEventsConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string QueueName = "user.events";

        public UserEventsConsumer(
            ILogger<UserEventsConsumer> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            ConfigureQueue();
        }

        private void ConfigureQueue()
        {
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
                _logger.LogInformation("User events consumer is stopping..."));

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserEventsConsumer>>();

                try
                {
                    var body = ea.Body.ToArray();
                    var messageBody = Encoding.UTF8.GetString(body);

                    logger.LogInformation("Raw message received: {MessageBody}", messageBody);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    var message = System.Text.Json.JsonSerializer.Deserialize<UserCreatedMessage>(messageBody, options);

                    if (message == null)
                    {
                        logger.LogError("Failed to deserialize message");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("Processing user event: {UserId}", message?.Id);

                    if (message == null)
                    {
                        _logger.LogWarning("Mensagem inválida recebida");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    // Verificar se o usuário já existe
                    var existingUser = await dbContext.Users
                        .FirstOrDefaultAsync(u => u.Id == message.Id);

                    if (existingUser != null)
                    {
                        // Atualizar usuário existente
                        existingUser.Email = message.Email;
                        existingUser.Username = message.Username;
                        dbContext.Users.Update(existingUser);
                    }
                    else
                    {
                        // Criar novo usuário
                        var newUser = new User
                        {
                            Id = message.Id,
                            Email = message.Email,
                            Username = message.Username
                        };
                        await dbContext.Users.AddAsync(newUser);
                    }

                    await dbContext.SaveChangesAsync();
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("User synchronized: {Email}", message.Email);
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "JSON deserialization error");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error processing message");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }

    public class UserCreatedMessage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }
}
