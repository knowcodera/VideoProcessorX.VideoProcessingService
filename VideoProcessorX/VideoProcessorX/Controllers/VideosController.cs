using FFMpegCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Infrastructure.Persistence;
using VideoProcessorX.WebApi.DTOs.Video;

namespace VideoProcessorX.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env; // para obter caminhos locais

        public VideosController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

        [HttpGet("{id}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadZip(int id)
        {
            // Identifica o usuário
            var userId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);

            // Localiza o vídeo no BD
            var video = await _context.Videos.FindAsync(id);
            if (video == null)
                return NotFound("Vídeo não encontrado.");

            // Verifica se pertence ao usuário logado
            if (video.UserId != userId)
                return Forbid("Você não tem permissão para acessar este vídeo.");

            // Verifica se o status é COMPLETED e se o ZipPath existe
            if (video.Status != "COMPLETED" || string.IsNullOrEmpty(video.ZipPath))
                return BadRequest("O vídeo ainda não foi processado ou não há arquivo zip disponível.");

            // Checa se o arquivo realmente existe no disco
            if (!System.IO.File.Exists(video.ZipPath))
                return NotFound("Arquivo .zip não foi encontrado no servidor.");

            // Extrai o nome do arquivo a partir do caminho
            var fileName = Path.GetFileName(video.ZipPath);

            // Define o tipo de conteúdo (MIME)
            var contentType = "application/octet-stream";

            // Retorna o arquivo físico para download
            return PhysicalFile(video.ZipPath, contentType, fileName);
        }



        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(500_000_000)] // 500 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
        public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDto dto)
        {
            // Recupera o ID do usuário logado pelos claims
            var userId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);

            // Checa se veio mesmo um arquivo
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("Nenhum arquivo enviado.");

            // Define uma pasta local para armazenar
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Gera um nome único (UUID) para evitar conflitos
            var uniqueFileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            // Salva fisicamente o arquivo
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

            // Cria o registro no banco
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

            // Processamento do vídeo (simples e síncrono para MVP)
            try
            {
                // Atualiza status para PROCESSING
                video.Status = "PROCESSING";
                _context.Videos.Update(video);
                await _context.SaveChangesAsync();

                // Simula extrair frames / gerar zip
                var zipPath = await GenerateFramesZipAsync(video.FilePath, video.Id);

                // Atualiza status para COMPLETED
                video.ZipPath = zipPath;
                video.Status = "COMPLETED";
                video.ProcessedAt = DateTime.UtcNow;

                _context.Videos.Update(video);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Em caso de erro
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

        private async Task<string> GenerateFramesZipAsync(string videoPath, int videoId)
        {
            // 1) Obter info do vídeo (duração, etc.)
            var videoInfo = await FFProbe.AnalyseAsync(videoPath);
            var duration = videoInfo.Duration;

            // Pasta onde salvaremos os frames temporários
            var framesFolder = Path.Combine(_env.ContentRootPath, "frames", $"video_{videoId}");
            if (!Directory.Exists(framesFolder))
                Directory.CreateDirectory(framesFolder);

            // 2) Extrair frames a cada 20s
            var interval = TimeSpan.FromSeconds(20);
            for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += interval)
            {
                Console.WriteLine($"Processando frame: {currentTime}");

                var frameFileName = $"frame_{currentTime.TotalSeconds:F0}.png";
                var frameFullPath = Path.Combine(framesFolder, frameFileName);

                // Gera um frame (snapshot)
                FFMpeg.Snapshot(videoPath, frameFullPath,
                    new System.Drawing.Size(1920, 1080), // tamanho do frame
                    currentTime);
            }

            // 3) Gerar .zip de todos os frames
            var zipsFolder = Path.Combine(_env.ContentRootPath, "zips");
            if (!Directory.Exists(zipsFolder))
                Directory.CreateDirectory(zipsFolder);

            var zipFileName = $"video_{videoId}_{Guid.NewGuid()}.zip";
            var zipFullPath = Path.Combine(zipsFolder, zipFileName);

            // Criar o arquivo ZIP
            using (var zipStream = new FileStream(zipFullPath, FileMode.Create))
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create))
            {
                // Adiciona cada frame ao ZIP
                var frameFiles = Directory.GetFiles(framesFolder, "*.png");
                foreach (var frame in frameFiles)
                {
                    var entryName = Path.GetFileName(frame); // Nome do arquivo dentro do ZIP
                    var entry = archive.CreateEntry(entryName);

                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(frame, FileMode.Open, FileAccess.Read))
                    {
                        await fileStream.CopyToAsync(entryStream); // Copia o conteúdo do arquivo para o ZIP
                    }
                }
            }

            // Opcional: se quiser limpar os frames temporários
            Directory.Delete(framesFolder, recursive: true);

            return zipFullPath;
        }
    }
}
