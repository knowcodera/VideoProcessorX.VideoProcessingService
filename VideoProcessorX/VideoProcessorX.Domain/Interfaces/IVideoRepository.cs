using VideoProcessorX.Domain.Entities;

namespace VideoProcessorX.Domain.Interfaces
{
    public interface IVideoRepository
    {
        Task<Video> GetByIdAsync(int videoId);
        Task<List<Video>> GetByUserIdAsync(int userId);
        Task CreateAsync(Video video);
        Task UpdateAsync(Video video);
    }
}
