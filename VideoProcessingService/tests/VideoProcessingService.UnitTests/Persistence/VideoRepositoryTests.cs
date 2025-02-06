using Microsoft.EntityFrameworkCore;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Infrastructure.Persistence;

namespace VideoProcessingService.UnitTests.Persistence
{
    public class VideoRepositoryTests
    {
        private readonly AppDbContext _context;
        private readonly VideoRepository _repository;

        public VideoRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;

            _context = new AppDbContext(options);
            _repository = new VideoRepository(_context);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddVideo()
        {
            var video = new Video { Id = 1, OriginalFileName = "test.mp4", FilePath = "path.mp4", Status = "PENDING" };
            await _repository.CreateAsync(video);
            var result = await _repository.GetByIdAsync(video.Id);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyVideo()
        {
            var video = new Video { Id = 1, OriginalFileName = "test.mp4", FilePath = "path.mp4", Status = "PENDING" };
            await _repository.CreateAsync(video);

            video.Status = "COMPLETED";
            await _repository.UpdateAsync(video);

            var updated = await _repository.GetByIdAsync(video.Id);
            Assert.Equal("COMPLETED", updated.Status);
        }
    }
}
