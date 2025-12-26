using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Supabase S3-compatible storage service implementation
    /// </summary>
    public class SupabaseStorageService : IStorageService, IDisposable
    {
        private readonly IAmazonS3 _s3Client;
        private readonly SupabaseStorageOptions _options;
        private readonly ILogger<SupabaseStorageService> _logger;
        private bool _disposed;

        public SupabaseStorageService(
            IOptions<SupabaseStorageOptions> options,
            ILogger<SupabaseStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // Configure S3 client for Supabase
            var config = new AmazonS3Config
            {
                ServiceURL = _options.S3Endpoint,
                ForcePathStyle = true, // Required for Supabase S3 compatibility
                SignatureVersion = "v4"
            };

            _s3Client = new AmazonS3Client(
                _options.AccessKeyId,
                _options.SecretAccessKey,
                config
            );

            _logger.LogInformation("? Supabase Storage Service initialized with bucket: {BucketName}", _options.BucketName);
        }

        public async Task<StorageUploadResult> UploadFileAsync(IFormFile file, string? folder = null, string? customFileName = null)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return StorageUploadResult.FailureResult("File is empty or null");
                }

                if (file.Length > _options.MaxFileSizeBytes)
                {
                    var maxSizeMB = _options.MaxFileSizeBytes / (1024 * 1024);
                    return StorageUploadResult.FailureResult($"File size exceeds maximum allowed size of {maxSizeMB}MB");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_options.AllowedImageExtensions.Contains(extension))
                {
                    return StorageUploadResult.FailureResult($"File type {extension} is not allowed");
                }

                // Generate unique filename
                var fileName = customFileName ?? Path.GetFileNameWithoutExtension(file.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid():N}{extension}";
                
                // Build the object key (path in bucket)
                var objectKey = string.IsNullOrEmpty(folder) 
                    ? uniqueFileName 
                    : $"{folder.TrimEnd('/')}/{uniqueFileName}";

                // Upload to S3
                using var stream = file.OpenReadStream();
                
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = objectKey,
                    BucketName = _options.BucketName,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.PublicRead // Make file publicly accessible
                };

                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest);

                var publicUrl = GetPublicUrl(objectKey);
                
                _logger.LogInformation("? File uploaded successfully: {Key} -> {Url}", objectKey, publicUrl);

                return StorageUploadResult.SuccessResult(publicUrl, objectKey, file.Length, file.ContentType);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "? S3 error uploading file: {FileName}", file?.FileName);
                return StorageUploadResult.FailureResult($"S3 error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error uploading file: {FileName}", file?.FileName);
                return StorageUploadResult.FailureResult($"Upload error: {ex.Message}");
            }
        }

        public async Task<StorageUploadResult> UploadFileAsync(Stream stream, string fileName, string contentType, string? folder = null)
        {
            try
            {
                if (stream == null || stream.Length == 0)
                {
                    return StorageUploadResult.FailureResult("Stream is empty or null");
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}{extension}";
                
                var objectKey = string.IsNullOrEmpty(folder) 
                    ? uniqueFileName 
                    : $"{folder.TrimEnd('/')}/{uniqueFileName}";

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = objectKey,
                    BucketName = _options.BucketName,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest);

                var publicUrl = GetPublicUrl(objectKey);

                _logger.LogInformation("? File uploaded from stream: {Key}", objectKey);

                return StorageUploadResult.SuccessResult(publicUrl, objectKey, stream.Length, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error uploading file from stream: {FileName}", fileName);
                return StorageUploadResult.FailureResult($"Upload error: {ex.Message}");
            }
        }

        public async Task<StorageUploadResult> UploadFileAsync(byte[] data, string fileName, string contentType, string? folder = null)
        {
            using var stream = new MemoryStream(data);
            return await UploadFileAsync(stream, fileName, contentType, folder);
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                var objectKey = ExtractKeyFromUrl(fileUrl);
                if (string.IsNullOrEmpty(objectKey))
                {
                    _logger.LogWarning("Could not extract object key from URL: {Url}", fileUrl);
                    return false;
                }

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = objectKey
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);

                _logger.LogInformation("? File deleted: {Key}", objectKey);
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "? S3 error deleting file: {Url}", fileUrl);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error deleting file: {Url}", fileUrl);
                return false;
            }
        }

        public async Task<bool> DeleteFilesAsync(IEnumerable<string> fileUrls)
        {
            try
            {
                var keys = fileUrls
                    .Select(ExtractKeyFromUrl)
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Select(k => new KeyVersion { Key = k })
                    .ToList();

                if (!keys.Any())
                {
                    return true;
                }

                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Objects = keys
                };

                var response = await _s3Client.DeleteObjectsAsync(deleteRequest);

                _logger.LogInformation("? Deleted {Count} files", response.DeletedObjects.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error deleting multiple files");
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                var objectKey = ExtractKeyFromUrl(fileUrl);
                if (string.IsNullOrEmpty(objectKey))
                {
                    return false;
                }

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _options.BucketName,
                    Key = objectKey
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error checking if file exists: {Url}", fileUrl);
                return false;
            }
        }

        public string GetPublicUrl(string fileKey)
        {
            return $"{_options.PublicUrl}/{fileKey}";
        }

        public async Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expiration)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _options.BucketName,
                    Key = fileKey,
                    Expires = DateTime.UtcNow.Add(expiration)
                };

                return await Task.FromResult(_s3Client.GetPreSignedURL(request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error generating presigned URL for: {Key}", fileKey);
                throw;
            }
        }

        private string? ExtractKeyFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            // If it's already a key (not a URL), return as is
            if (!url.StartsWith("http"))
            {
                return url;
            }

            // Extract key from public URL
            // Format: {ProjectUrl}/storage/v1/object/public/{BucketName}/{key}
            var publicPrefix = $"{_options.PublicUrl}/";
            if (url.StartsWith(publicPrefix))
            {
                return url.Substring(publicPrefix.Length);
            }

            // Try to extract from any URL format
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                
                // Remove leading /storage/v1/object/public/{bucket}/
                var prefix = $"/storage/v1/object/public/{_options.BucketName}/";
                if (path.StartsWith(prefix))
                {
                    return path.Substring(prefix.Length);
                }

                // Fallback: return the last part of the path
                return Path.GetFileName(path);
            }
            catch
            {
                return Path.GetFileName(url);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _s3Client?.Dispose();
            }

            _disposed = true;
        }
    }
}
