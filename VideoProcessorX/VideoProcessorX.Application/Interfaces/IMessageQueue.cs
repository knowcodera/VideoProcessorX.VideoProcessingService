namespace VideoProcessingService.Application.Interfaces
{
    public interface IMessageQueue
    {
        Task PublishAsync(string queueName, object message);
    }
}
