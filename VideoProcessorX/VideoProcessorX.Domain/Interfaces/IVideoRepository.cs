using VideoProcessingService.Domain.Entities;

namespace VideoProcessingService.Domain.Interfaces
{
    public interface IVideoRepository
    {
        Task<Video> GetByIdAsync(int videoId);
        Task CreateAsync(Video video);
        Task UpdateAsync(Video video);
    }
}
