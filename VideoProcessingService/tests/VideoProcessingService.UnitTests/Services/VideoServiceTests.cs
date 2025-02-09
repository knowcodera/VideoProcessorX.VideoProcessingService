using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using VideoProcessingService.Application.Services;
using VideoProcessingService.Domain.Interfaces;
using VideoProcessingService.Infrastructure.Persistence;

namespace VideoProcessingService.UnitTests.Services
{
    public class VideoServiceTests
    {
        //private readonly Mock<IWebHostEnvironment> _envMock;
        //private readonly Mock<IVideoRepository> _loggerMock;
        //private readonly VideoService _videoService;

        //public VideoServiceTests()
        //{
        //    _envMock = new Mock<IWebHostEnvironment>();
        //    _loggerMock = new Mock<ILoIVideoRepository>>();
        //    _videoService = new VideoService(_envMock.Object, _loggerMock.Object);
        //}

        //[Fact]
        //public async Task GenerateFramesZipAsync_ShouldCreateZip()
        //{
        //    // Arrange
        //    var videoPath = Path.GetTempFileName();
        //    var videoId = 1;

        //    // Act
        //    var result = await _videoService.GenerateFramesZipAsync(videoPath, videoId);

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.EndsWith(".zip", result);
        //}
    }
}
