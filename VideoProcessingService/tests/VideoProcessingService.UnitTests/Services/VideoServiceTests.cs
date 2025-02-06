using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using VideoProcessingService.Application.Services;

namespace VideoProcessingService.UnitTests.Services
{
    public class VideoServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<ILogger<VideoService>> _loggerMock;
        private readonly VideoService _videoService;

        public VideoServiceTests()
        {
            _envMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<VideoService>>();
            _videoService = new VideoService(_envMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GenerateFramesZipAsync_ShouldCreateZip()
        {
            // Arrange
            var videoPath = Path.GetTempFileName();
            var videoId = 1;

            // Act
            var result = await _videoService.GenerateFramesZipAsync(videoPath, videoId);

            // Assert
            Assert.NotNull(result);
            Assert.EndsWith(".zip", result);
        }
    }
}
