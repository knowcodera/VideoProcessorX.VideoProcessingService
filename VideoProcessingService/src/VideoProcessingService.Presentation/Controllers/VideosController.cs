using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Domain.Entities;
using VideoProcessingService.Infrastructure.Data;
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

        public VideosController(
            AppDbContext context,
            IWebHostEnvironment env,
            IVideoService videoService,
            IMessageQueue messageQueue) 
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

            if (!System.IO.File.Exists(video.ZipPath))
                return NotFound("Arquivo ZIP não encontrado");

            var fileStream = new FileStream(video.ZipPath, FileMode.Open, FileAccess.Read);
            return File(fileStream, "application/zip", $"{Path.GetFileNameWithoutExtension(video.OriginalFileName)}_frames.zip");
        }
    }
}
