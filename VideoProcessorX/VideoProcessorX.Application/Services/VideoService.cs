using FFMpegCore;
using Microsoft.AspNetCore.Hosting;
using VideoProcessingService.Application.Interfaces;

namespace VideoProcessingService.Application.Services
{
    public class VideoService : IVideoService
{
    private readonly IWebHostEnvironment _environment;

    public VideoService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> GenerateFramesZipAsync(string videoPath, int videoId)
    {
        var framesFolder = Path.Combine(_environment.ContentRootPath, "frames", $"video_{videoId}");
        Directory.CreateDirectory(framesFolder);

        var interval = TimeSpan.FromSeconds(20);
        var videoInfo = await FFProbe.AnalyseAsync(videoPath);
        var duration = videoInfo.Duration;
        
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += interval)
        {
            var frameFileName = $"frame_{currentTime.TotalSeconds:F0}.png";
            var frameFullPath = Path.Combine(framesFolder, frameFileName);
            FFMpeg.Snapshot(videoPath, frameFullPath, new System.Drawing.Size(1920, 1080), currentTime);
        }

        var zipFilePath = Path.Combine(_environment.ContentRootPath, "zips", $"video_{videoId}_{Guid.NewGuid()}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(zipFilePath));
        
        using (var zipStream = new FileStream(zipFilePath, FileMode.Create))
        using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            foreach (var frame in Directory.GetFiles(framesFolder, "*.png"))
            {
                var entry = archive.CreateEntry(Path.GetFileName(frame));
                await using var entryStream = entry.Open();
                await using var fileStream = new FileStream(frame, FileMode.Open, FileAccess.Read);
                await fileStream.CopyToAsync(entryStream);
            }
        }

        Directory.Delete(framesFolder, true);
        return zipFilePath;
    }
}
}
