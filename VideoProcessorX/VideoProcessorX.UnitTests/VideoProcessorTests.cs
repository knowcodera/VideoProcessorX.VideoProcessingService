using Moq;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Domain.Interfaces;
using VideoProcessingService.Domain.Services;

namespace VideoProcessorX.UnitTests
{
    public class VideoProcessorTests
    {
        private readonly Mock<IVideoRepository> _videoRepositoryMock;
        private readonly VideoProcessor _videoProcessor;

        public VideoProcessorTests()
        {
            _videoRepositoryMock = new Mock<IVideoRepository>();
            _videoProcessor = new VideoProcessor(_videoRepositoryMock.Object);
        }

        [Fact]
        public async Task ProcessVideoAsync_Should_Set_Status_Completed_When_Success()
        {
            // Arrange
            var video = new Video { Id = 1, Status = "PENDING" };
            _videoRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(video);

            // Act
            var result = await _videoProcessor.ProcessVideoAsync(1);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("COMPLETED", video.Status);
            _videoRepositoryMock.Verify(repo => repo.UpdateAsync(video), Times.Once);
        }
    }

}
