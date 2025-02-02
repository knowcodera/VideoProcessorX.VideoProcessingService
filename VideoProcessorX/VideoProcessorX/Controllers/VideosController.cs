using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Presentation.DTOs.Video;

namespace VideoProcessorX.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IVideoService _videoService;
        private readonly IMessageQueue _messageQueue; // Novo campo

        public VideosController(
            AppDbContext context,
            IWebHostEnvironment env,
            IVideoService videoService,
            IMessageQueue messageQueue) // Injeção do IMessageQueue
        {
            _context = context;
            _env = env;
            _videoService = videoService;
            _messageQueue = messageQueue;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserVideos()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);

            var videos = await _context.Videos
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new
                {
                    v.Id,
                    v.OriginalFileName,
                    v.Status,
                    v.ZipPath,
                    v.CreatedAt,
                    v.ProcessedAt
                })
                .ToListAsync();

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

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao salvar o arquivo: {ex.Message}");
            }

            var video = new Video
            {
                UserId = userId,
                OriginalFileName = dto.File.FileName,
                FilePath = fullPath,
                Status = "PENDING",
                ZipPath = string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            try
            {
                // Enfileira o processamento
                await _messageQueue.PublishAsync("video.process", new
                {
                    VideoId = video.Id
                });

                return Ok(new
                {
                    message = "Upload concluído. Processamento em segundo plano.",
                    videoId = video.Id,
                    status = video.Status
                });
            }
            catch (Exception ex)
            {
                // Em caso de erro no enfileiramento
                video.Status = "ERROR";
                _context.Videos.Update(video);
                await _context.SaveChangesAsync();

                return StatusCode(500, $"Erro ao enfileirar o processamento: {ex.Message}");
            }
        }
    }
}
