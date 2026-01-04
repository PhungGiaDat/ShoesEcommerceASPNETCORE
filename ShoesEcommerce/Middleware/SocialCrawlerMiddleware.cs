using System.Text.RegularExpressions;

namespace ShoesEcommerce.Middleware
{
    /// <summary>
    /// Middleware to allow social media crawlers (Facebook, Twitter, etc.) 
    /// to access pages for Open Graph tag scraping without authentication
    /// </summary>
    public class SocialCrawlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SocialCrawlerMiddleware> _logger;

        // Known social media crawler user agents
        private static readonly string[] SocialCrawlerPatterns = new[]
        {
            "facebookexternalhit",
            "Facebot",
            "Twitterbot",
            "LinkedInBot",
            "Pinterest",
            "Slackbot",
            "TelegramBot",
            "WhatsApp",
            "Discordbot",
            "Googlebot",
            "bingbot"
        };

        public SocialCrawlerMiddleware(RequestDelegate next, ILogger<SocialCrawlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "";

            // Check if this is a social media crawler
            var isSocialCrawler = SocialCrawlerPatterns.Any(pattern => 
                userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (isSocialCrawler)
            {
                _logger.LogInformation(
                    "Social crawler detected: {UserAgent}, Path: {Path}",
                    userAgent, context.Request.Path);

                // Set a flag to indicate this is a social crawler request
                context.Items["IsSocialCrawler"] = true;

                // Skip authentication for social crawlers on public pages
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                
                // Allow crawlers on public product pages
                if (path.StartsWith("/san-pham") || 
                    path.StartsWith("/product") ||
                    path.StartsWith("/khuyen-mai") ||
                    path == "/" ||
                    path.StartsWith("/home"))
                {
                    // Mark as anonymous request to bypass auth checks
                    context.Items["AllowAnonymous"] = true;
                }
            }

            await _next(context);

            // Log if we returned an error to a social crawler
            if (isSocialCrawler && context.Response.StatusCode >= 400)
            {
                _logger.LogWarning(
                    "Social crawler received error response: {StatusCode}, UserAgent: {UserAgent}, Path: {Path}",
                    context.Response.StatusCode, userAgent, context.Request.Path);
            }
        }
    }

    /// <summary>
    /// Extension method for registering the middleware
    /// </summary>
    public static class SocialCrawlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseSocialCrawlerSupport(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SocialCrawlerMiddleware>();
        }
    }
}
