namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Configuration options for Supabase S3-compatible storage
    /// </summary>
    public class SupabaseStorageOptions
    {
        public const string SectionName = "SupabaseStorage";

        /// <summary>
        /// Supabase project URL (e.g., https://xxxxx.supabase.co)
        /// </summary>
        public string ProjectUrl { get; set; } = string.Empty;

        /// <summary>
        /// S3 endpoint URL (e.g., https://xxxxx.storage.supabase.co/storage/v1/s3)
        /// </summary>
        public string S3Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Access Key ID for S3 authentication
        /// </summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>
        /// Secret Access Key for S3 authentication
        /// </summary>
        public string SecretAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// Default bucket name for storing files
        /// </summary>
        public string BucketName { get; set; } = "images";

        /// <summary>
        /// AWS region (use any value for Supabase, e.g., "us-east-1")
        /// </summary>
        public string Region { get; set; } = "us-east-1";

        /// <summary>
        /// Base URL for accessing public files
        /// </summary>
        public string PublicUrl => $"{ProjectUrl}/storage/v1/object/public/{BucketName}";

        /// <summary>
        /// Maximum file size in bytes (default: 5MB)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

        /// <summary>
        /// Allowed file extensions for images
        /// </summary>
        public string[] AllowedImageExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    }
}
