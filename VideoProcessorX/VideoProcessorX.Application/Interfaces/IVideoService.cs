using Microsoft.AspNetCore.Http;
using VideoProcessingService.Application.Common;

namespace VideoProcessingService.Application.Interfaces
{
    public interface IVideoService
    {
        Task<string> GenerateFramesZipAsync(string videoPath, int videoId);
    }
}
