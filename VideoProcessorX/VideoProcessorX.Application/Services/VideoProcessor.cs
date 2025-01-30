using VideoProcessingService.Application.Common;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Domain.Interfaces;

namespace VideoProcessingService.Application.Services
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
            // Obtém o vídeo
            var video = await _videoRepository.GetByIdAsync(videoId);
            if (video == null)
                return Result.Failure("Vídeo não encontrado.");

            try
            {
                // Atualiza para status de processamento
                video.Status = "PROCESSING";
                await _videoRepository.UpdateAsync(video);

                // Simula o processamento
                await Task.Delay(5000); // Simula tempo de processamento

                // Atualiza para status de concluído
                video.Status = "COMPLETED";
                video.ProcessedAt = DateTime.UtcNow;
                await _videoRepository.UpdateAsync(video);

                return Result.Success();
            }
            catch (Exception ex)
            {
                // Atualiza para status de erro em caso de falha
                video.Status = "ERROR";
                await _videoRepository.UpdateAsync(video);

                return Result.Failure($"Erro no processamento: {ex.Message}");
            }
        }
    }
}
