namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Configuration options for Social Media Chat integrations (Facebook, Zalo)
    /// </summary>
    public class SocialChatOptions
    {
        /// <summary>
        /// The configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "SocialChat";

        /// <summary>
        /// Facebook Messenger Chat Plugin configuration
        /// </summary>
        public FacebookMessengerOptions Facebook { get; set; } = new();

        /// <summary>
        /// Zalo OA Chat configuration
        /// </summary>
        public ZaloChatOptions Zalo { get; set; } = new();
    }

    /// <summary>
    /// Facebook Messenger Chat Plugin options
    /// </summary>
    public class FacebookMessengerOptions
    {
        /// <summary>
        /// Whether Facebook Messenger chat is enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Facebook Page ID for the chat plugin
        /// </summary>
        public string PageId { get; set; } = string.Empty;

        /// <summary>
        /// Facebook App ID (optional, for advanced features)
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Theme color for the chat plugin (hex color without #)
        /// </summary>
        public string ThemeColor { get; set; } = "0084FF";

        /// <summary>
        /// Logged in greeting message
        /// </summary>
        public string LoggedInGreeting { get; set; } = "Xin chào! Chúng tôi có thể giúp gì cho bạn?";

        /// <summary>
        /// Logged out greeting message
        /// </summary>
        public string LoggedOutGreeting { get; set; } = "Xin chào! Đăng nhập Facebook để chat với chúng tôi!";
    }

    /// <summary>
    /// Zalo OA Chat options
    /// </summary>
    public class ZaloChatOptions
    {
        /// <summary>
        /// Whether Zalo chat is enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Zalo OA ID (Official Account ID)
        /// </summary>
        public string OaId { get; set; } = string.Empty;

        /// <summary>
        /// Welcome message when chat opens
        /// </summary>
        public string WelcomeMessage { get; set; } = "Xin chào! Shoes Ecommerce có thể giúp gì cho bạn?";

        /// <summary>
        /// Auto popup delay in seconds (0 = disabled)
        /// </summary>
        public int AutoPopupDelay { get; set; } = 0;
    }
}
