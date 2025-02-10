namespace VideoProcessingService.UnitTests.Messaging
{
    using Microsoft.Extensions.Logging;
    using Moq;
    using RabbitMQ.Client;
    using VideoProcessingService.Infrastructure.Messaging;

    public class RabbitMqMessageQueueTests
    {
        [Fact]
        public async Task PublishAsync_ShouldSendMessageToChannel()
        {
            // Arrange
            var channelMock = new Mock<IModel>();
            var loggerMock = new Mock<ILogger<RabbitMqMessageQueue>>();

            var queue = new RabbitMqMessageQueue(channelMock.Object, loggerMock.Object);
            var message = new VideoProcessMessage(1);

            // Act
            await queue.PublishAsync("test.queue", message);

            // Assert
            channelMock.Verify(c => c.BasicPublish(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()),
                Times.Once);
        }
    }
}

