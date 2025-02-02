namespace VideoProcessingService.Application.Interfaces
{
    public interface IVideoService
    {
        Task<string> GenerateFramesZipAsync(string videoPath, int videoId);
    }
}
