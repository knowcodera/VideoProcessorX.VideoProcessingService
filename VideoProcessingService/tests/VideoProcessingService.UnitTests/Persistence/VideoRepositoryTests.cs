using Microsoft.EntityFrameworkCore;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Infrastructure.Persistence;

namespace VideoProcessingService.UnitTests.Persistence
{
    public class VideoRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly VideoRepository _repository;

        public VideoRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new VideoRepository(_context);

            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddVideoToDatabase()
        {
            // Arrange
            var video = new Video
            {
                Id = 1,
                OriginalFileName = "test.mp4",
                Status = "PENDING"
            };

            // Act
            await _repository.CreateAsync(video);
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.mp4", result.OriginalFileName);
        }

        [Fact]
        public async Task GetVideosByUserIdAsync_ShouldReturnFilteredVideos()
        {
            // Arrange
            var userId = 1;
            var video = new Video
            {
                Id = 1,
                UserId = userId,
                OriginalFileName = "test.mp4"
            };

            await _repository.CreateAsync(video);

            // Act
            var result = await _repository.GetVideosByUserIdAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("test.mp4", ((dynamic)result.First()).OriginalFileName);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
