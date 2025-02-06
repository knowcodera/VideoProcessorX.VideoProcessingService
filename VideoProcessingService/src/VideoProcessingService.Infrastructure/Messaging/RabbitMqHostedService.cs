using Microsoft.Extensions.Hosting;

namespace VideoProcessingService.Infrastructure.Messaging
{
    public class RabbitMqHostedService : IHostedService
    {
        private readonly RabbitMqListener _listener;

        public RabbitMqHostedService(RabbitMqListener listener)
        {
            _listener = listener;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _listener.Stop();
            return Task.CompletedTask;
        }
    }
}
