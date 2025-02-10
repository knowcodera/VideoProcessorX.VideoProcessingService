using Microsoft.AspNetCore.Http;

namespace VideoProcessingService.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile formFile, string fileName);
        Task<string> UploadFileAsync(string localFilePath, string fileName);
        Task DownloadToLocalAsync(string blobPath, string localFilePath);
        Task<Stream> GetFileStreamAsync(string blobPath);
        Task<bool> FileExistsAsync(string blobPath);
        Task<string> ComputeFileHashAsync(IFormFile formFile);
    }
}
