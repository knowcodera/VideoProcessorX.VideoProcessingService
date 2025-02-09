using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Presentation.DTOs.Video;
using VideoProcessingService.WebApi.Controllers;

namespace VideoProcessingService.UnitTests.Controllers
{
    public class VideoControllerTests
    {
        private readonly Mock<AppDbContext> _dbContextMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IVideoService> _videoServiceMock;
        private readonly Mock<IMessageQueue> _messageQueueMock;
        private readonly VideosController _controller;

        public VideoControllerTests()
        {
            _dbContextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _envMock = new Mock<IWebHostEnvironment>();
            _videoServiceMock = new Mock<IVideoService>();
            _messageQueueMock = new Mock<IMessageQueue>();

            _controller = new VideosController(
                _dbContextMock.Object,
                _envMock.Object,
                _videoServiceMock.Object,
                _messageQueueMock.Object
            );
        }

        [Fact]
        public async Task UploadVideo_DeveRetornarOk_QuandoUploadBemSucedido()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("dummy content");
            writer.Flush();
            stream.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns("video.mp4");

            var dto = new VideoUploadDto { File = fileMock.Object };

            // Simulação da pasta de uploads
            var uploadFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            _envMock.Setup(e => e.ContentRootPath).Returns(uploadFolderPath);
            Directory.CreateDirectory(uploadFolderPath);

            // Simulação da fila de mensagens
            _messageQueueMock.Setup(mq => mq.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadVideo(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task UploadVideo_DeveRetornarBadRequest_QuandoArquivoNulo()
        {
            // Arrange
            var dto = new VideoUploadDto { File = null };

            // Act
            var result = await _controller.UploadVideo(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task DownloadZip_DeveRetornarArquivoZip_QuandoVideoProcessado()
        {
            // Arrange
            var videoId = 1;
            var zipPath = Path.Combine(Directory.GetCurrentDirectory(), "video.zip");

            var video = new VideoProcessingService.Domain.Entities.Video
            {
                Id = videoId,
                Status = "COMPLETED",
                ZipPath = zipPath
            };

            _dbContextMock.Setup(db => db.Videos.FindAsync(videoId))
                          .ReturnsAsync(video);

            File.WriteAllText(zipPath, "Dummy zip content");

            // Act
            var result = await _controller.DownloadZip(videoId);

            // Assert
            Assert.IsType<FileStreamResult>(result);
        }

        [Fact]
        public async Task DownloadZip_DeveRetornarNotFound_QuandoVideoNaoProcessado()
        {
            // Arrange
            var videoId = 1;
            var video = new VideoProcessingService.Domain.Entities.Video
            {
                Id = videoId,
                Status = "PENDING"
            };

            _dbContextMock.Setup(db => db.Videos.FindAsync(videoId))
                          .ReturnsAsync(video);

            // Act
            var result = await _controller.DownloadZip(videoId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }
    }
}
