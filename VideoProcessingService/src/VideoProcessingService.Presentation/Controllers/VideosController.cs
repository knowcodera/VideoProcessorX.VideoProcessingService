using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Domain.Interfaces;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Infrastructure.Messaging;
using VideoProcessingService.Presentation.DTOs.Video;

namespace VideoProcessingService.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IVideoService _videoService;
        private readonly IMessageQueue _messageQueue;
        private readonly IFileStorageService _fileStorageService;
        private readonly IVideoRepository _videoRepository;

        public VideosController(
            AppDbContext context,
            IWebHostEnvironment env,
            IVideoService videoService,
            IMessageQueue messageQueue,
            IFileStorageService fileStorageService,
            IVideoRepository videoRepository)
        {
            _context = context;
            _env = env;
            _videoService = videoService;
            _messageQueue = messageQueue;
            _fileStorageService = fileStorageService;
            _videoRepository = videoRepository;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserVideos()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);
            var videos = await _videoService.GetUserVideosAsync(userId);
            return Ok(videos);
        }

        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(500_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
        public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDto dto)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("Nenhum arquivo enviado.");

            var fileHash = await _fileStorageService.ComputeFileHashAsync(dto.File);
            var existingVideo = await _videoRepository.GetByUserAndHashAsync(userId, fileHash);

            string blobUrl;
            if (existingVideo != null)
            {
                blobUrl = existingVideo.FilePath;
            }
            else
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
                try
                {
                    blobUrl = await _fileStorageService.UploadFileAsync(dto.File, uniqueFileName);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Erro ao enviar para Blob Storage: {ex.Message}");
                }
            }

            var video = new Video
            {
                UserId = userId,
                OriginalFileName = dto.File.FileName,
                FilePath = blobUrl,
                FileHash = fileHash,
                Status = "PENDING",
                ZipPath = string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            try
            {
                await _messageQueue.PublishAsync("video.process", new VideoProcessMessage(video.Id));

                return Ok(new
                {
                    message = "Upload concluído. Processamento em segundo plano.",
                    videoId = video.Id,
                    status = video.Status
                });
            }
            catch (Exception ex)
            {
                video.Status = "ERROR";
                _context.Videos.Update(video);
                await _context.SaveChangesAsync();

                return StatusCode(500, $"Erro ao enfileirar o processamento: {ex.Message}");
            }
        }

        [HttpGet("download/{videoId}")]
        [Authorize]
        public async Task<IActionResult> DownloadZip(int videoId)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null || video.Status != "COMPLETED")
                return NotFound("Arquivo não encontrado ou não processado");

            // Agora o ZipPath guarda a URL do blob
            if (string.IsNullOrEmpty(video.ZipPath))
                return NotFound("Arquivo ZIP não encontrado no registro do vídeo.");

            // Verifica se o blob existe
            var exists = await _fileStorageService.FileExistsAsync(video.ZipPath);
            if (!exists)
            {
                return NotFound("Arquivo ZIP não encontrado no Blob Storage.");
            }

            // Baixar como stream e devolver para o cliente
            var stream = await _fileStorageService.GetFileStreamAsync(video.ZipPath);
            if (stream == null)
            {
                return NotFound("Não foi possível obter o Stream do Blob Storage.");
            }

            // Sugerimos um nome para o ZIP
            var downloadFileName = $"{Path.GetFileNameWithoutExtension(video.OriginalFileName)}_frames.zip";

            return File(stream, "application/zip", downloadFileName);
        }
    }
}
