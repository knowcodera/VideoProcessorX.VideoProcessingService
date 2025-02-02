using FFMpegCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using VideoProcessingService.Application.Interfaces;
using Xabe.FFmpeg;

namespace VideoProcessingService.Application.Services
{
    public class VideoService : IVideoService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VideoService> _logger;

        public VideoService(
            IWebHostEnvironment environment,
            ILogger<VideoService> logger)
        {
            _environment = environment;
            _logger = logger;

            // Configuração do FFmpeg
            FFmpeg.SetExecutablesPath(Path.Combine(_environment.ContentRootPath, "ffmpeg"));
        }

        public async Task<string> GenerateFramesZipAsync(string videoPath, int videoId)
        {
            _logger.LogInformation($"Iniciando processamento do vídeo {videoId}");

            var tempFolder = Path.Combine(Path.GetTempPath(), $"video_{videoId}_{Guid.NewGuid()}");
            var zipFileName = $"video_{videoId}_{DateTime.Now:yyyyMMddHHmmss}.zip";
            var zipFilePath = Path.Combine(_environment.ContentRootPath, "zips", zipFileName);

            try
            {
                Directory.CreateDirectory(tempFolder);
                Directory.CreateDirectory(Path.GetDirectoryName(zipFilePath));

                _logger.LogDebug($"Extraindo frames para: {tempFolder}");

                var interval = TimeSpan.FromSeconds(20);
                var videoInfo = await FFmpeg.GetMediaInfo(videoPath);

                var conversionTasks = new List<Task>();
                var frameCount = (int)(videoInfo.Duration.TotalSeconds / interval.TotalSeconds);

                // Processamento paralelo de frames
                await Parallel.ForEachAsync(
                    Enumerable.Range(0, frameCount),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    async (frameNumber, ct) =>
                    {
                        var currentTime = TimeSpan.FromSeconds(frameNumber * interval.TotalSeconds);
                        var frameFileName = $"frame_{frameNumber:0000}.png";
                        var frameFullPath = Path.Combine(tempFolder, frameFileName);

                        await FFMpeg.SnapshotAsync(
                            videoPath,
                            frameFullPath,
                            new System.Drawing.Size(1920, 1080),
                            currentTime);
                    });

                _logger.LogDebug($"Criando arquivo ZIP: {zipFilePath}");

                // Criação assíncrona do ZIP
                await using (var zipStream = new FileStream(zipFilePath, FileMode.Create))
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var frame in Directory.GetFiles(tempFolder, "*.png"))
                        {
                            var entryName = Path.GetFileName(frame);
                            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                            await using (var entryStream = entry.Open())
                            await using (var fileStream = new FileStream(frame, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                }

                _logger.LogInformation($"Processamento concluído para vídeo {videoId}");
                return zipFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar vídeo {videoId}");
                throw;
            }
            finally
            {
                // Limpeza garantida dos arquivos temporários
                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                        _logger.LogDebug($"Pasta temporária removida: {tempFolder}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Falha ao limpar arquivos temporários para vídeo {videoId}");
                }
            }
        }
    }
}
