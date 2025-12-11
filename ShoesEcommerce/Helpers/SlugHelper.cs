using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ShoesEcommerce.Helpers
{
    /// <summary>
    /// Helper class for generating SEO-friendly URL slugs
    /// </summary>
    public static class SlugHelper
    {
        /// <summary>
        /// Converts a string to a URL-friendly slug
        /// </summary>
        /// <param name="title">The title to convert</param>
        /// <returns>A lowercase, hyphen-separated URL-friendly string</returns>
        public static string ToSlug(this string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            // Convert to lowercase
            title = title.ToLowerInvariant();

            // Handle Vietnamese special characters
            title = ConvertVietnameseCharacters(title);

            // Normalize the string (decompose characters like é -> e)
            title = title.Normalize(NormalizationForm.FormD);
            
            // Remove diacritical marks (accents)
            var stringBuilder = new StringBuilder();
            foreach (var c in title)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            title = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // Replace special characters with their equivalents
            title = title.Replace("&", "and")
                        .Replace("@", "at")
                        .Replace("#", "sharp")
                        .Replace("+", "plus")
                        .Replace("$", "dollar")
                        .Replace("%", "percent");

            // Remove any characters that are not alphanumeric, spaces, or hyphens
            title = Regex.Replace(title, @"[^a-z0-9\s-]", "");

            // Replace multiple spaces with single space
            title = Regex.Replace(title, @"\s+", " ");

            // Trim leading/trailing spaces
            title = title.Trim();

            // Replace spaces with hyphens
            title = title.Replace(' ', '-');

            // Remove duplicate hyphens
            title = Regex.Replace(title, @"-+", "-");

            // Trim leading/trailing hyphens
            title = title.Trim('-');

            return title;
        }

        /// <summary>
        /// Converts Vietnamese characters to their ASCII equivalents
        /// </summary>
        private static string ConvertVietnameseCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Vietnamese character mappings
            var vietnameseChars = new Dictionary<string, string>
            {
                // Lowercase vowels with diacritics
                { "?", "d" },
                { "à", "a" }, { "á", "a" }, { "?", "a" }, { "ã", "a" }, { "?", "a" },
                { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" },
                { "â", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" },
                { "è", "e" }, { "é", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" },
                { "ê", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" },
                { "ì", "i" }, { "í", "i" }, { "?", "i" }, { "?", "i" }, { "?", "i" },
                { "ò", "o" }, { "ó", "o" }, { "?", "o" }, { "õ", "o" }, { "?", "o" },
                { "ô", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" },
                { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" },
                { "ù", "u" }, { "ú", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" },
                { "?", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" },
                { "?", "y" }, { "ý", "y" }, { "?", "y" }, { "?", "y" }, { "?", "y" },
                
                // Uppercase vowels with diacritics
                { "?", "d" },
                { "À", "a" }, { "Á", "a" }, { "?", "a" }, { "Ã", "a" }, { "?", "a" },
                { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" },
                { "Â", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" }, { "?", "a" },
                { "È", "e" }, { "É", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" },
                { "Ê", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" }, { "?", "e" },
                { "Ì", "i" }, { "Í", "i" }, { "?", "i" }, { "?", "i" }, { "?", "i" },
                { "Ò", "o" }, { "Ó", "o" }, { "?", "o" }, { "Õ", "o" }, { "?", "o" },
                { "Ô", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" },
                { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" }, { "?", "o" },
                { "Ù", "u" }, { "Ú", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" },
                { "?", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" }, { "?", "u" },
                { "?", "y" }, { "Ý", "y" }, { "?", "y" }, { "?", "y" }, { "?", "y" }
            };

            foreach (var kvp in vietnameseChars)
            {
                text = text.Replace(kvp.Key, kvp.Value);
            }

            return text;
        }

        /// <summary>
        /// Generates a unique slug by appending an ID if needed
        /// </summary>
        /// <param name="title">The title to convert</param>
        /// <param name="id">The unique identifier to append</param>
        /// <returns>A URL-friendly slug with optional ID suffix</returns>
        public static string ToSlugWithId(this string title, int id)
        {
            var slug = title.ToSlug();
            return string.IsNullOrEmpty(slug) ? id.ToString() : $"{slug}-{id}";
        }

        /// <summary>
        /// Extracts the ID from a slug that ends with -id format
        /// </summary>
        /// <param name="slugWithId">The slug containing an ID suffix</param>
        /// <returns>The extracted ID, or 0 if not found</returns>
        public static int ExtractIdFromSlug(string slugWithId)
        {
            if (string.IsNullOrWhiteSpace(slugWithId))
                return 0;

            // Try to extract ID from the end of the slug (format: "product-name-123")
            var lastDashIndex = slugWithId.LastIndexOf('-');
            if (lastDashIndex > 0 && lastDashIndex < slugWithId.Length - 1)
            {
                var idPart = slugWithId.Substring(lastDashIndex + 1);
                if (int.TryParse(idPart, out int id))
                {
                    return id;
                }
            }

            // If the slug is just a number
            if (int.TryParse(slugWithId, out int directId))
            {
                return directId;
            }

            return 0;
        }

        /// <summary>
        /// Validates if a slug matches the expected format for a given title and ID
        /// </summary>
        public static bool ValidateSlug(string slug, string expectedTitle, int expectedId)
        {
            var expectedSlug = expectedTitle.ToSlugWithId(expectedId);
            return string.Equals(slug, expectedSlug, StringComparison.OrdinalIgnoreCase);
        }
    }
}
