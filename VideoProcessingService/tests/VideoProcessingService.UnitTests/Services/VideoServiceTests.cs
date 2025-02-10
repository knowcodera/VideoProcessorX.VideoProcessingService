using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Application.Services;
using VideoProcessingService.Domain.Interfaces;

namespace VideoProcessingService.UnitTests.Services
{
    public class VideoServiceTests : IDisposable
    {
        private readonly Mock<IVideoRepository> _videoRepoMock;
        private readonly Mock<IFileStorageService> _fileStorageMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly VideoService _service;
        private readonly string _tempDir;

        public VideoServiceTests()
        {
            _videoRepoMock = new Mock<IVideoRepository>();
            _fileStorageMock = new Mock<IFileStorageService>();
            _envMock = new Mock<IWebHostEnvironment>();

            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            _envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);

            _service = new VideoService(_envMock.Object, Mock.Of<ILogger<VideoService>>(),
                _videoRepoMock.Object, _fileStorageMock.Object);
        }

        [Fact]
        public async Task GenerateFramesZipAsync_ShouldProcessVideoSuccessfully()
        {
            // Arrange
            var videoUrl = "https://example.com/video.mp4";
            var videoId = 1;
            var zipUrl = "https://example.com/result.zip";

            _fileStorageMock.Setup(f => f.DownloadToLocalAsync(videoUrl, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _fileStorageMock.Setup(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(zipUrl);

            // Act
            var result = await _service.GenerateFramesZipAsync(videoUrl, videoId);

            // Assert
            Assert.Equal(zipUrl, result);
            _fileStorageMock.Verify(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GenerateFramesZipAsync_ShouldHandleStorageErrors()
        {
            // Arrange
            var videoUrl = "https://example.com/video.mp4";
            var videoId = 1;

            _fileStorageMock.Setup(f => f.DownloadToLocalAsync(videoUrl, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Storage error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GenerateFramesZipAsync(videoUrl, videoId));
        }

        [Fact]
        public async Task GetUserVideosAsync_ShouldReturnVideos()
        {
            // Arrange
            var userId = 1;
            var expectedVideos = new List<object> { new { Id = 1, Name = "test" } };

            _videoRepoMock.Setup(r => r.GetVideosByUserIdAsync(userId))
                .ReturnsAsync(expectedVideos);

            // Act
            var result = await _service.GetUserVideosAsync(userId);

            // Assert
            Assert.Equal(expectedVideos, result);
        }

        public void Dispose()
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
