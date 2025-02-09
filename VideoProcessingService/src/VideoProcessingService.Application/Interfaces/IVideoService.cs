namespace VideoProcessingService.Application.Interfaces
{
    using VideoProcessingService.Domain.Entities;

    public interface IVideoService
    {
        Task<string> GenerateFramesZipAsync(string videoPath, int videoId);
        Task<IEnumerable<Object>> GetUserVideosAsync(int userId);
    }
}
