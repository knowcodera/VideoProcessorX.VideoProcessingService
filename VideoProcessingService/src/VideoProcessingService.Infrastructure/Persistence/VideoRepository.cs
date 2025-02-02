using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Domain.Interfaces;
using VideoProcessingService.Infrastructure.Data;

namespace VideoProcessingService.Infrastructure.Persistence
{
    public class VideoRepository : IVideoRepository
    {
        private readonly AppDbContext _context;

        public VideoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Video> GetByIdAsync(int videoId)
        {
            return await _context.Videos.FindAsync(videoId);
        }

        public async Task CreateAsync(Video video)
        {
            _context.Videos.Add(video);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Video video)
        {
            _context.Videos.Update(video);
            await _context.SaveChangesAsync();
        }
    }
}
