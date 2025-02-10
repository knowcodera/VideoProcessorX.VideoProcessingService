using VideoProcessingService.Domain.Entities;

namespace VideoProcessingService.Domain.Interfaces
{
    public interface IVideoRepository
    {
        Task<Video> GetByIdAsync(int videoId);
        Task CreateAsync(Video video);
        Task UpdateAsync(Video video);
        Task<IEnumerable<Object>> GetVideosByUserIdAsync(int userId);
        Task<Video> GetByUserAndHashAsync(int userId, string fileHash);
    }
}
