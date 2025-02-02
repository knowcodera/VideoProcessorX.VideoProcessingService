using Microsoft.Extensions.DependencyInjection;
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
            // Aqui você inicia a escuta da fila RabbitMQ.
            _listener.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Chamado quando o serviço de hospedagem (Host) está parando.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Aqui você pára a escuta e libera recursos, se necessário.
            _listener.Stop();
            return Task.CompletedTask;
        }
    }
}
