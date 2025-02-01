using VideoProcessingService.Domain.Common;
using VideoProcessingService.Domain.Interfaces;

namespace VideoProcessingService.Domain.Services
{
    public class VideoProcessor : IVideoProcessor
    {
        private readonly IVideoRepository _videoRepository;

        public VideoProcessor(IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository ?? throw new ArgumentNullException(nameof(videoRepository));
        }

        public async Task<Result> ProcessVideoAsync(int videoId)
        {
            var video = await _videoRepository.GetByIdAsync(videoId);
            if (video == null)
                return Result.Failure("Vídeo não encontrado.");

            try
            {
                video.Status = "PROCESSING";
                await _videoRepository.UpdateAsync(video);

                await Task.Delay(5000); 

                video.Status = "COMPLETED";
                video.ProcessedAt = DateTime.UtcNow;
                await _videoRepository.UpdateAsync(video);

                return Result.Success();
            }
            catch (Exception ex)
            {
                video.Status = "ERROR";
                await _videoRepository.UpdateAsync(video);

                return Result.Failure($"Erro no processamento: {ex.Message}");
            }
        }
    }
}
