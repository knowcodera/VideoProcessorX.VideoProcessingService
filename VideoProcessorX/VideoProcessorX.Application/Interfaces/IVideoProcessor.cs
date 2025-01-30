using VideoProcessingService.Application.Common;
using VideoProcessingService.Domain.Entities;

namespace VideoProcessingService.Application.Interfaces
{
    public interface IVideoProcessor
    {
        Task<Result> ProcessVideoAsync(int videoId);
    }
}
