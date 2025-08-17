using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private const string ImageFolder = "images";
        private const string ProductVariantFolder = "product-variants";

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
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

                // Create upload directory
                var uploadPath = CreateUploadDirectory(subFolder);

                // Generate unique filename
                var fileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative URL
                var relativePath = Path.Combine(ImageFolder, subFolder, fileName)
                    .Replace('\\', '/');
                
                return $"/{relativePath}";
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

                // Create specific subfolder for product variants
                var subFolder = Path.Combine(ProductVariantFolder, $"product-{productId}");
                var uploadPath = CreateUploadDirectory(subFolder);

                _logger.LogInformation("Upload path created: {UploadPath}", uploadPath);

                // Generate descriptive filename
                var fileName = GenerateProductVariantFileName(file.FileName, productId, color, size);
                var filePath = Path.Combine(uploadPath, fileName);

                _logger.LogInformation("Saving file to: {FilePath}", filePath);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Verify file was saved
                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException("File was not saved successfully");
                }

                _logger.LogInformation("File saved successfully: {FileName}, Size: {FileSize}", fileName, file.Length);

                // Return relative URL
                var relativePath = Path.Combine(ImageFolder, subFolder, fileName)
                    .Replace('\\', '/');
                
                var imageUrl = $"/{relativePath}";
                _logger.LogInformation("Returning image URL: {ImageUrl}", imageUrl);
                
                return imageUrl;
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
                if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/"))
                    return false;

                var physicalPath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
                
                if (File.Exists(physicalPath))
                {
                    await Task.Run(() => File.Delete(physicalPath));
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

        private string CreateUploadDirectory(string subFolder)
        {
            try
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, ImageFolder);
                
                if (!string.IsNullOrEmpty(subFolder))
                {
                    uploadPath = Path.Combine(uploadPath, subFolder);
                }

                _logger.LogInformation("Creating directory: {UploadPath}", uploadPath);

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                    _logger.LogInformation("Directory created successfully: {UploadPath}", uploadPath);
                }
                else
                {
                    _logger.LogInformation("Directory already exists: {UploadPath}", uploadPath);
                }

                // Verify directory was created and is writable
                if (!Directory.Exists(uploadPath))
                {
                    throw new DirectoryNotFoundException($"Failed to create directory: {uploadPath}");
                }

                // Test write permissions
                var testFile = Path.Combine(uploadPath, $"test_{Guid.NewGuid()}.tmp");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    _logger.LogInformation("Directory write test successful: {UploadPath}", uploadPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Directory write test failed: {UploadPath}", uploadPath);
                    throw new UnauthorizedAccessException($"No write permission for directory: {uploadPath}", ex);
                }

                return uploadPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating upload directory for subfolder: {SubFolder}", subFolder);
                throw;
            }
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            
            // Sanitize filename
            fileName = SanitizeFileName(fileName);
            
            // Add timestamp and GUID for uniqueness
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            
            return $"{fileName}_{timestamp}_{uniqueId}{extension}";
        }

        private string GenerateProductVariantFileName(string originalFileName, int productId, string color, string size)
        {
            var extension = Path.GetExtension(originalFileName);
            
            // Sanitize inputs
            var sanitizedColor = SanitizeFileName(color);
            var sanitizedSize = SanitizeFileName(size);
            
            // Create descriptive filename
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return $"product_{productId}_{sanitizedColor}_{sanitizedSize}_{timestamp}{extension}";
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Remove extra spaces and convert to lowercase
            return sanitized.Trim().Replace(" ", "_").ToLowerInvariant();
        }
    }
}