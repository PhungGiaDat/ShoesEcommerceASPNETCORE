using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;
using System.Text;
using System.Globalization;

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

            // Configure S3 client for Supabase with proper settings
            var config = new AmazonS3Config
            {
                ServiceURL = _options.S3Endpoint,
                ForcePathStyle = true,
                //RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName("auto")
            };

            // Create credentials
            var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);

            _s3Client = new AmazonS3Client(credentials, config);

            _logger.LogInformation("Supabase Storage Service initialized");
            _logger.LogInformation("   - S3 Endpoint: {Endpoint}", _options.S3Endpoint);
            _logger.LogInformation("   - Bucket: {BucketName}", _options.BucketName);
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

                // Generate unique filename - sanitize properly (ASCII only!)
                var fileName = customFileName ?? Path.GetFileNameWithoutExtension(file.FileName);
                fileName = SanitizeToAscii(fileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid():N}{extension}";
                
                // Build the object key (path in bucket) - ensure no special characters
                var sanitizedFolder = string.IsNullOrEmpty(folder) ? "" : SanitizeFolderPath(folder);
                var objectKey = string.IsNullOrEmpty(sanitizedFolder) 
                    ? uniqueFileName 
                    : $"{sanitizedFolder}/{uniqueFileName}";

                _logger.LogInformation("Uploading to Supabase: {Key}", objectKey);

                // Read file into memory stream
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Create put request with minimal settings for Supabase compatibility
                var putRequest = new PutObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = objectKey,
                    InputStream = memoryStream,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    //DisablePayloadSigning = true // Important for Supabase S3
                };

                var response = await _s3Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var publicUrl = GetPublicUrl(objectKey);
                    _logger.LogInformation("File uploaded successfully: {Url}", publicUrl);
                    return StorageUploadResult.SuccessResult(publicUrl, objectKey, file.Length, file.ContentType ?? "application/octet-stream");
                }
                else
                {
                    _logger.LogError("Upload failed with status code: {StatusCode}", response.HttpStatusCode);
                    return StorageUploadResult.FailureResult($"Upload failed with status code: {response.HttpStatusCode}");
                }
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "S3 error: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
                return StorageUploadResult.FailureResult($"S3 error: {ex.Message} (Code: {ex.ErrorCode})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
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
                var sanitizedName = SanitizeToAscii(Path.GetFileNameWithoutExtension(fileName));
                var uniqueFileName = $"{sanitizedName}_{Guid.NewGuid():N}{extension}";
                
                var sanitizedFolder = string.IsNullOrEmpty(folder) ? "" : SanitizeFolderPath(folder);
                var objectKey = string.IsNullOrEmpty(sanitizedFolder) 
                    ? uniqueFileName 
                    : $"{sanitizedFolder}/{uniqueFileName}";

                // Ensure stream is at beginning
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                // Copy to memory stream for reliable upload
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var putRequest = new PutObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = objectKey,
                    InputStream = memoryStream,
                    ContentType = contentType,
                    DisablePayloadSigning = true
                };

                var response = await _s3Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var publicUrl = GetPublicUrl(objectKey);
                    _logger.LogInformation("File uploaded from stream: {Key}", objectKey);
                    return StorageUploadResult.SuccessResult(publicUrl, objectKey, memoryStream.Length, contentType);
                }
                else
                {
                    return StorageUploadResult.FailureResult($"Upload failed with status: {response.HttpStatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file from stream: {FileName}", fileName);
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

                _logger.LogInformation("File deleted: {Key}", objectKey);
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "S3 error deleting file: {Url}", fileUrl);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Url}", fileUrl);
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

                _logger.LogInformation("Deleted {Count} files", response.DeletedObjects.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multiple files");
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
                _logger.LogError(ex, "Error checking if file exists: {Url}", fileUrl);
                return false;
            }
        }

        public string GetPublicUrl(string fileKey)
        {
            // Supabase public URL format: {ProjectUrl}/storage/v1/object/public/{BucketName}/{Key}
            return $"{_options.ProjectUrl}/storage/v1/object/public/{_options.BucketName}/{fileKey}";
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
                _logger.LogError(ex, "Error generating presigned URL for: {Key}", fileKey);
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

                // Try alternative prefix without bucket
                var altPrefix = "/storage/v1/object/public/";
                if (path.StartsWith(altPrefix))
                {
                    var remaining = path.Substring(altPrefix.Length);
                    var slashIndex = remaining.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        return remaining.Substring(slashIndex + 1);
                    }
                }

                return Path.GetFileName(path);
            }
            catch
            {
                return Path.GetFileName(url);
            }
        }

        /// <summary>
        /// Sanitize text to ASCII only - removes all non-ASCII characters including Vietnamese diacritics
        /// This is critical for S3 signature compatibility
        /// </summary>
        private string SanitizeToAscii(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "file";

            // Normalize the string to decompose characters (separate base chars from diacritics)
            var normalized = text.Normalize(NormalizationForm.FormD);
            
            // Build result with only ASCII letters, digits, underscore and hyphen
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                
                // Skip combining marks (diacritics)
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;
                
                // Keep ASCII letters and digits
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
                // Replace common Vietnamese d with d
                else if (c == '\u0111' || c == '\u0110') // đ or Đ
                {
                    sb.Append('d');
                }
                // Replace spaces and some punctuation with underscore
                else if (c == ' ' || c == '-' || c == '_')
                {
                    sb.Append('_');
                }
                // Skip all other characters
            }

            var result = sb.ToString().ToLowerInvariant();
            
            // Clean up multiple underscores
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }
            
            result = result.Trim('_');

            // Limit length
            if (result.Length > 50)
            {
                result = result.Substring(0, 50);
            }

            return string.IsNullOrEmpty(result) ? "file" : result;
        }

        /// <summary>
        /// Sanitize folder path - each segment must be ASCII only
        /// </summary>
        private string SanitizeFolderPath(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return "";

            // Split by / and sanitize each part
            var parts = folder.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var sanitizedParts = parts.Select(p => SanitizeToAscii(p)).Where(p => !string.IsNullOrEmpty(p));
            return string.Join("/", sanitizedParts);
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
