using Microsoft.EntityFrameworkCore;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Domain.Interfaces;
using VideoProcessorX.Infrastructure.Persistence;

namespace VideoProcessorX.Infrastructure.Repositories
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

        public async Task<List<Video>> GetByUserIdAsync(int userId)
        {
            return await _context.Videos
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
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
