using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;

namespace ShoesEcommerce.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IStorageService? _storageService;
        private readonly SupabaseStorageOptions? _storageOptions;
        private readonly bool _useCloudStorage;
        
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private const string ImageFolder = "images";
        private const string ProductVariantFolder = "product-variants";

        public FileUploadService(
            IWebHostEnvironment environment, 
            ILogger<FileUploadService> logger,
            IStorageService? storageService = null,
            IOptions<SupabaseStorageOptions>? storageOptions = null)
        {
            _environment = environment;
            _logger = logger;
            _storageService = storageService;
            _storageOptions = storageOptions?.Value;
            
            // Use cloud storage if configured
            _useCloudStorage = _storageOptions != null 
                && !string.IsNullOrEmpty(_storageOptions.AccessKeyId) 
                && !string.IsNullOrEmpty(_storageOptions.SecretAccessKey);

            if (_useCloudStorage)
            {
                _logger.LogInformation("?? FileUploadService using Supabase cloud storage");
            }
            else
            {
                _logger.LogInformation("?? FileUploadService using local file storage");
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string subFolder = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is null or empty");

                // Validate file
                var validationResult = ValidateImageFile(file);
                if (!validationResult.IsValid)
                    throw new ArgumentException(validationResult.ErrorMessage);

                // Use cloud storage if available
                if (_useCloudStorage && _storageService != null)
                {
                    return await UploadToCloudAsync(file, subFolder);
                }

                // Fallback to local storage
                return await UploadToLocalAsync(file, subFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image file");
                throw;
            }
        }

        public async Task<string> UploadProductVariantImageAsync(IFormFile file, int productId, string color, string size)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file provided for product variant image upload");
                    return string.Empty;
                }

                // Validate file first
                var validationResult = ValidateImageFile(file);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("File validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                    throw new ArgumentException(validationResult.ErrorMessage);
                }

                var subFolder = $"{ProductVariantFolder}/product-{productId}";

                // Use cloud storage if available
                if (_useCloudStorage && _storageService != null)
                {
                    var customFileName = GenerateProductVariantFileName(file.FileName, productId, color, size);
                    customFileName = Path.GetFileNameWithoutExtension(customFileName);
                    
                    var result = await _storageService.UploadFileAsync(file, subFolder, customFileName);
                    
                    if (result.Success && !string.IsNullOrEmpty(result.Url))
                    {
                        _logger.LogInformation("? Product variant image uploaded to cloud: {Url}", result.Url);
                        return result.Url;
                    }
                    else
                    {
                        _logger.LogWarning("?? Cloud upload failed: {Error}, falling back to local", result.ErrorMessage);
                    }
                }

                // Fallback to local storage
                return await UploadProductVariantToLocalAsync(file, productId, color, size);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product variant image for Product {ProductId}, Color: {Color}, Size: {Size}", 
                    productId, color, size);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return false;

                // Check if it's a cloud URL
                if (imageUrl.StartsWith("http"))
                {
                    if (_storageService != null)
                    {
                        return await _storageService.DeleteFileAsync(imageUrl);
                    }
                    return false;
                }

                // Local file deletion
                if (!imageUrl.StartsWith("/"))
                    return false;

                var physicalPath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
                
                if (File.Exists(physicalPath))
                {
                    await Task.Run(() => File.Delete(physicalPath));
                    _logger.LogInformation("??? Deleted local file: {Path}", physicalPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ImageUrl}", imageUrl);
                return false;
            }
        }

        public ValidationResult ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new ValidationResult { IsValid = false, ErrorMessage = "File is required" };

            // Check file size
            if (file.Length > MaxFileSize)
                return new ValidationResult { IsValid = false, ErrorMessage = $"File size must be less than {MaxFileSize / (1024 * 1024)}MB" };

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return new ValidationResult { IsValid = false, ErrorMessage = $"Only {string.Join(", ", _allowedExtensions)} files are allowed" };

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return new ValidationResult { IsValid = false, ErrorMessage = "Invalid file type" };

            return new ValidationResult { IsValid = true };
        }

        #region Cloud Storage Methods

        private async Task<string> UploadToCloudAsync(IFormFile file, string subFolder)
        {
            var result = await _storageService!.UploadFileAsync(file, subFolder);
            
            if (result.Success && !string.IsNullOrEmpty(result.Url))
            {
                _logger.LogInformation("? Image uploaded to cloud: {Url}", result.Url);
                return result.Url;
            }
            
            _logger.LogWarning("?? Cloud upload failed: {Error}, falling back to local storage", result.ErrorMessage);
            return await UploadToLocalAsync(file, subFolder);
        }

        #endregion

        #region Local Storage Methods

        private async Task<string> UploadToLocalAsync(IFormFile file, string subFolder)
        {
            var uploadPath = CreateUploadDirectory(subFolder);
            var fileName = GenerateUniqueFileName(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine(ImageFolder, subFolder, fileName).Replace('\\', '/');
            var imageUrl = $"/{relativePath}";
            
            _logger.LogInformation("?? Image uploaded to local: {Url}", imageUrl);
            return imageUrl;
        }

        private async Task<string> UploadProductVariantToLocalAsync(IFormFile file, int productId, string color, string size)
        {
            var subFolder = Path.Combine(ProductVariantFolder, $"product-{productId}");
            var uploadPath = CreateUploadDirectory(subFolder);

            var fileName = GenerateProductVariantFileName(file.FileName, productId, color, size);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("File was not saved successfully");
            }

            var relativePath = Path.Combine(ImageFolder, subFolder, fileName).Replace('\\', '/');
            var imageUrl = $"/{relativePath}";
            
            _logger.LogInformation("?? Product variant image uploaded to local: {Url}", imageUrl);
            return imageUrl;
        }

        private string CreateUploadDirectory(string subFolder)
        {
            var uploadPath = Path.Combine(_environment.WebRootPath, ImageFolder);
            
            if (!string.IsNullOrEmpty(subFolder))
            {
                uploadPath = Path.Combine(uploadPath, subFolder);
            }

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
                _logger.LogInformation("?? Directory created: {Path}", uploadPath);
            }

            return uploadPath;
        }

        #endregion

        #region Filename Generation

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            fileName = SanitizeFileName(fileName);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            
            return $"{fileName}_{timestamp}_{uniqueId}{extension}";
        }

        private string GenerateProductVariantFileName(string originalFileName, int productId, string color, string size)
        {
            var extension = Path.GetExtension(originalFileName);
            var sanitizedColor = SanitizeFileName(color);
            var sanitizedSize = SanitizeFileName(size);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return $"product_{productId}_{sanitizedColor}_{sanitizedSize}_{timestamp}{extension}";
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Trim().Replace(" ", "_").ToLowerInvariant();
        }

        #endregion
    }
}