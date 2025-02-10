using System.IO.Compression;
using System.Runtime.InteropServices;
using FFMpegCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Domain.Interfaces;
using Xabe.FFmpeg;

namespace VideoProcessingService.Application.Services
{
    public class VideoService : IVideoService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VideoService> _logger;
        private readonly IVideoRepository _videoRepository;
        private readonly IFileStorageService _fileStorageService;

        // Subpasta base onde guardaremos vídeos, frames e zips
        private readonly string _baseDir;

        public VideoService(
            IWebHostEnvironment environment,
            ILogger<VideoService> logger,
            IVideoRepository videoRepository,
            IFileStorageService fileStorageService)
        {
            _environment = environment;
            _logger = logger;
            _videoRepository = videoRepository;
            _fileStorageService = fileStorageService;

            // Configura FFmpeg
            ConfigureFfmpegPath();

            // Define a subpasta "zips" (ou "uploads", etc.) dentro do ContentRootPath
            _baseDir = Path.Combine(_environment.ContentRootPath, "zips");
            Directory.CreateDirectory(_baseDir); // Garante que existe
        }

        private void ConfigureFfmpegPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Se no Windows, assumindo que ffmpeg.exe e ffprobe.exe estejam dentro do ContentRootPath
                FFmpeg.SetExecutablesPath(_environment.ContentRootPath);
            }
            else
            {
                // Em Docker Linux, assumindo ffmpeg instalado em /usr/bin
                FFmpeg.SetExecutablesPath("/usr/bin");
            }
        }

        public async Task<string> GenerateFramesZipAsync(string videoBlobUrl, int videoId)
        {
            _logger.LogInformation("Iniciando processamento do vídeo {videoId}", videoId);

            // 1) Montamos caminhos locais
            // Extensão (caso o link termine com .mp4, .mov etc.)
            var ext = Path.GetExtension(videoBlobUrl);
            if (string.IsNullOrEmpty(ext)) ext = ".mp4";

            // Nome do arquivo de vídeo baixado
            var localVideoPath = Path.Combine(_baseDir, $"video_{videoId}_{Guid.NewGuid()}{ext}");

            // Subpasta para frames
            var framesFolder = Path.Combine(_baseDir, $"frames_{videoId}_{Guid.NewGuid()}");
            Directory.CreateDirectory(framesFolder);

            // Nome final do ZIP
            var zipFileName = $"video_{videoId}_{DateTime.Now:yyyyMMddHHmmss}.zip";
            var localZipPath = Path.Combine(_baseDir, zipFileName);

            // 2) Baixa do blob
            try
            {
                await _fileStorageService.DownloadToLocalAsync(videoBlobUrl, localVideoPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar vídeo do blob: {videoBlobUrl}", videoBlobUrl);
                throw;
            }

            try
            {
                // Intervalo fixo de 20s (pode mudar, ou tornar configurável)
                var interval = TimeSpan.FromSeconds(5);

                // Obtém info do vídeo
                var mediaInfo = await FFmpeg.GetMediaInfo(localVideoPath);
                var totalSec = mediaInfo.Duration.TotalSeconds;

                // Quantidade de frames = total / 20s
                var frameCount = (int)(totalSec / interval.TotalSeconds);
                if (frameCount < 1) frameCount = 1; // mínimo de 1 frame

                _logger.LogDebug("Extraindo {frameCount} frames (interval={interval}s) do vídeo {videoId}",
                                 frameCount, interval.TotalSeconds, videoId);

                // 3) Extração paralela de frames
                await Parallel.ForEachAsync(
                    Enumerable.Range(0, frameCount),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    async (frameNumber, ct) =>
                    {
                        var currentTime = TimeSpan.FromSeconds(frameNumber * interval.TotalSeconds);
                        var frameName = $"frame_{frameNumber:0000}.png";
                        var framePath = Path.Combine(framesFolder, frameName);

                        await FFMpeg.SnapshotAsync(
                            localVideoPath,
                            framePath,
                            new System.Drawing.Size(1920, 1080),
                            currentTime
                        );
                    }
                );

                _logger.LogDebug("Criando arquivo ZIP: {localZipPath}", localZipPath);

                // 4) Cria o ZIP com todos os frames
                using (var zipStream = new FileStream(localZipPath, FileMode.Create, FileAccess.Write))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false))
                {
                    var frames = Directory.GetFiles(framesFolder, "*.png", SearchOption.TopDirectoryOnly);
                    foreach (var frameFile in frames)
                    {
                        var entryName = Path.GetFileName(frameFile);
                        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                        using var entryStream = entry.Open();
                        using var fs = File.OpenRead(frameFile);
                        await fs.CopyToAsync(entryStream);
                    }
                }

                _logger.LogInformation("Processamento concluído para vídeo {videoId}", videoId);

                // 5) Upload do ZIP p/ Blob
                var blobZipFileName = $"{Guid.NewGuid()}_{zipFileName}";
                var zipBlobUrl = await _fileStorageService.UploadFileAsync(localZipPath, blobZipFileName);

                return zipBlobUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar vídeo {videoId}", videoId);
                throw;
            }
            finally
            {
                // 6) Limpeza local
                try
                {
                    if (File.Exists(localVideoPath))
                        File.Delete(localVideoPath);

                    if (Directory.Exists(framesFolder))
                        Directory.Delete(framesFolder, true);

                    if (File.Exists(localZipPath))
                        File.Delete(localZipPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao limpar arquivos temporários do vídeo {videoId}", videoId);
                }
            }
        }

        public async Task<IEnumerable<object>> GetUserVideosAsync(int userId)
        {
            return await _videoRepository.GetVideosByUserIdAsync(userId);
        }
    }
}
