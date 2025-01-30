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
        private readonly IWebHostEnvironment _env; // para obter caminhos locais
        private readonly IVideoService _videoService;

        public VideosController(AppDbContext context, IWebHostEnvironment env, IVideoService videoService)
        {
            _context = context;
            _env = env;
            _videoService = videoService;
        }

        [HttpGet]
        [Authorize]  // exije JWT no header Authorization
        public async Task<IActionResult> GetUserVideos()
        {
            // Identifica o ID do usuário logado pelos claims do JWT
            var userId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);

            // Consulta o BD, filtrando pelos vídeos do usuário
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

            // Retorna em formato JSON
            return Ok(videos);
        }


        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(500_000_000)] // 500 MB
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
                using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
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

                video.Status = "PROCESSING";
                _context.Videos.Update(video);
                await _context.SaveChangesAsync();


                var zipPath = await _videoService.GenerateFramesZipAsync(video.FilePath, video.Id);


                video.ZipPath = zipPath;
                video.Status = "COMPLETED";
                video.ProcessedAt = DateTime.UtcNow;

                _context.Videos.Update(video);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                video.Status = "ERROR";
                _context.Videos.Update(video);
                await _context.SaveChangesAsync();

                return StatusCode(500, $"Erro ao processar o vídeo: {ex.Message}");
            }

            return Ok(new
            {
                message = "Upload e processamento concluídos",
                videoId = video.Id,
                status = video.Status
            });
        }


    }
}
