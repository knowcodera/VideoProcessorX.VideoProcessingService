using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoProcessingService.Application.Interfaces;

namespace VideoProcessingService.Application.Services
{

    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration,
                                       ILogger<AzureBlobStorageService> logger)
        {
            _logger = logger;

            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"];

            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> UploadFileAsync(IFormFile formFile, string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            _logger.LogInformation($"Enviando arquivo para Blob Storage: {fileName}");

            using var stream = formFile.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = formFile.ContentType });

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadFileAsync(string localFilePath, string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            _logger.LogInformation($"Enviando arquivo local para Blob Storage: {localFilePath}");

            await blobClient.UploadAsync(localFilePath, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task DownloadToLocalAsync(string blobPath, string localFilePath)
        {
            string fileName = Path.GetFileName(new Uri(blobPath).AbsolutePath);
            var blobClient = _containerClient.GetBlobClient(fileName);

            _logger.LogInformation($"Baixando blob '{blobPath}' para '{localFilePath}'");

            await blobClient.DownloadToAsync(localFilePath);
        }

        public async Task<Stream> GetFileStreamAsync(string blobPath)
        {
            string fileName = Path.GetFileName(new Uri(blobPath).AbsolutePath);
            var blobClient = _containerClient.GetBlobClient(fileName);

            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                _logger.LogWarning($"Blob não encontrado: {blobPath}");
                return null;
            }

            var downloadInfo = await blobClient.DownloadAsync();
            return downloadInfo.Value.Content;
        }

        public async Task<bool> FileExistsAsync(string blobPath)
        {
            string fileName = Path.GetFileName(new Uri(blobPath).AbsolutePath);
            var blobClient = _containerClient.GetBlobClient(fileName);
            var exists = await blobClient.ExistsAsync();
            return exists.Value;
        }

        public async Task<string> ComputeFileHashAsync(IFormFile formFile)
        {
            using var sha256 = SHA256.Create();
            using var stream = formFile.OpenReadStream();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
