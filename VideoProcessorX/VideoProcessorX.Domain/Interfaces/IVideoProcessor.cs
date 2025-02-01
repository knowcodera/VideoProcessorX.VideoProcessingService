using VideoProcessingService.Domain.Common;

namespace VideoProcessingService.Domain.Interfaces
{
    public interface IVideoProcessor
    {
        Task<Result> ProcessVideoAsync(int videoId);
    }
}
