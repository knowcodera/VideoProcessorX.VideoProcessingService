namespace VideoProcessingService.Application.Interfaces
{
    public interface IVideoService
    {
        Task<string> GenerateFramesZipAsync(string videoBlobUrl, int videoId);
        Task<IEnumerable<Object>> GetUserVideosAsync(int userId);
    }
}
