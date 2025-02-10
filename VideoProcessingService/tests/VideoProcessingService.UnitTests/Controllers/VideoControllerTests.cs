using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Domain.Interfaces;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Presentation.DTOs.Video;
using VideoProcessingService.WebApi.Controllers;

namespace VideoProcessingService.UnitTests.Controllers
{
    public class VideosControllerTests : IDisposable
    {
        private readonly VideosController _controller;
        private readonly Mock<AppDbContext> _dbContextMock;
        private readonly Mock<IVideoService> _videoServiceMock;
        private readonly Mock<IMessageQueue> _messageQueueMock;
        private readonly Mock<IFileStorageService> _fileStorageMock;
        private readonly string _tempDir;

        public VideosControllerTests()
        {
            _dbContextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _videoServiceMock = new Mock<IVideoService>();
            _messageQueueMock = new Mock<IMessageQueue>();
            _fileStorageMock = new Mock<IFileStorageService>();

            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);

            _controller = new VideosController(
                _dbContextMock.Object,
                envMock.Object,
                _videoServiceMock.Object,
                _messageQueueMock.Object,
                _fileStorageMock.Object,
                Mock.Of<IVideoRepository>());
        }

        [Fact]
        public async Task UploadVideo_ShouldReturnOk_WhenValidFile()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("test.mp4");

            _fileStorageMock.Setup(f => f.ComputeFileHashAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("filehash");

            _fileStorageMock.Setup(f => f.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("https://storage/test.mp4");

            // Act
            var result = await _controller.UploadVideo(new VideoUploadDto { File = fileMock.Object });

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DownloadZip_ShouldReturnFile_WhenVideoExists()
        {
            // Arrange
            var videoId = 1;
            var video = new Video
            {
                Id = videoId,
                Status = "COMPLETED",
                ZipPath = "https://storage/result.zip"
            };

            _dbContextMock.Setup(d => d.Videos.FindAsync(videoId))
                .ReturnsAsync(video);

            _fileStorageMock.Setup(f => f.FileExistsAsync(video.ZipPath))
                .ReturnsAsync(true);

            _fileStorageMock.Setup(f => f.GetFileStreamAsync(video.ZipPath))
                .ReturnsAsync(new MemoryStream());

            // Act
            var result = await _controller.DownloadZip(videoId);

            // Assert
            Assert.IsType<FileStreamResult>(result);
        }

        public void Dispose()
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
