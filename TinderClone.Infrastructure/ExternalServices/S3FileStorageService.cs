using Amazon.S3;
using Amazon.S3.Model;
using TinderClone.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TinderClone.Infrastructure.ExternalServices;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3FileStorageService> _logger;
    private readonly string _bucketName;
    private readonly string? _cdnBaseUrl;

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _configuration = configuration;
        _logger = logger;
        _bucketName = configuration["S3:BucketName"] ?? "tinder-clone-photos";
        _cdnBaseUrl = configuration["CDN:BaseUrl"];
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GenerateFileKey(fileName);
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead // Для публичного доступа к фотографиям
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);
            
            var url = GetFileUrl(key);
            _logger.LogInformation("File uploaded to S3: {Key}, URL: {Url}", key, url);
            
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = ExtractKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Could not extract key from URL: {Url}", fileUrl);
                return false;
            }

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
            _logger.LogInformation("File deleted from S3: {Key}", key);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {Url}", fileUrl);
            return false;
        }
    }

    public async Task<Stream?> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = ExtractKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file from S3: {Url}", fileUrl);
            return null;
        }
    }

    public string GetFileUrl(string fileKey)
    {
        if (!string.IsNullOrEmpty(_cdnBaseUrl))
        {
            // Используем CDN URL если настроен
            return $"{_cdnBaseUrl.TrimEnd('/')}/{fileKey}";
        }

        // Иначе используем прямой S3 URL
        var region = _configuration["S3:Region"] ?? "us-east-1";
        return $"https://{_bucketName}.s3.{region}.amazonaws.com/{fileKey}";
    }

    private string GenerateFileKey(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var folder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return $"photos/{folder}/{uniqueFileName}";
    }

    private string? ExtractKeyFromUrl(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Если используется CDN URL
            if (!string.IsNullOrEmpty(_cdnBaseUrl) && url.StartsWith(_cdnBaseUrl))
            {
                return url.Replace(_cdnBaseUrl.TrimEnd('/'), "").TrimStart('/');
            }

            // Если используется прямой S3 URL
            var uri = new Uri(url);
            return uri.PathAndQuery.TrimStart('/');
        }
        catch
        {
            return null;
        }
    }
}

