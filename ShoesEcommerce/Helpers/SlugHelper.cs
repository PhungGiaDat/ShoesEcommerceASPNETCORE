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
        // Build the Vietnamese character map using Unicode code points to avoid encoding issues
        private static readonly Dictionary<char, string> VietnameseCharMap = BuildVietnameseCharMap();

        private static Dictionary<char, string> BuildVietnameseCharMap()
        {
            var map = new Dictionary<char, string>();
            
            // Lowercase d with stroke (?) - U+0111
            map['\u0111'] = "d";
            
            // Lowercase a with diacritics
            map['\u00E0'] = "a"; // à
            map['\u00E1'] = "a"; // á
            map['\u1EA3'] = "a"; // ?
            map['\u00E3'] = "a"; // ã
            map['\u1EA1'] = "a"; // ?
            map['\u0103'] = "a"; // ?
            map['\u1EB1'] = "a"; // ?
            map['\u1EAF'] = "a"; // ?
            map['\u1EB3'] = "a"; // ?
            map['\u1EB5'] = "a"; // ?
            map['\u1EB7'] = "a"; // ?
            map['\u00E2'] = "a"; // â
            map['\u1EA7'] = "a"; // ?
            map['\u1EA5'] = "a"; // ?
            map['\u1EA9'] = "a"; // ?
            map['\u1EAB'] = "a"; // ?
            map['\u1EAD'] = "a"; // ?
            
            // Lowercase e with diacritics
            map['\u00E8'] = "e"; // è
            map['\u00E9'] = "e"; // é
            map['\u1EBB'] = "e"; // ?
            map['\u1EBD'] = "e"; // ?
            map['\u1EB9'] = "e"; // ?
            map['\u00EA'] = "e"; // ê
            map['\u1EC1'] = "e"; // ?
            map['\u1EBF'] = "e"; // ?
            map['\u1EC3'] = "e"; // ?
            map['\u1EC5'] = "e"; // ?
            map['\u1EC7'] = "e"; // ?
            
            // Lowercase i with diacritics
            map['\u00EC'] = "i"; // ì
            map['\u00ED'] = "i"; // í
            map['\u1EC9'] = "i"; // ?
            map['\u0129'] = "i"; // ?
            map['\u1ECB'] = "i"; // ?
            
            // Lowercase o with diacritics
            map['\u00F2'] = "o"; // ò
            map['\u00F3'] = "o"; // ó
            map['\u1ECF'] = "o"; // ?
            map['\u00F5'] = "o"; // õ
            map['\u1ECD'] = "o"; // ?
            map['\u00F4'] = "o"; // ô
            map['\u1ED3'] = "o"; // ?
            map['\u1ED1'] = "o"; // ?
            map['\u1ED5'] = "o"; // ?
            map['\u1ED7'] = "o"; // ?
            map['\u1ED9'] = "o"; // ?
            map['\u01A1'] = "o"; // ?
            map['\u1EDD'] = "o"; // ?
            map['\u1EDB'] = "o"; // ?
            map['\u1EDF'] = "o"; // ?
            map['\u1EE1'] = "o"; // ?
            map['\u1EE3'] = "o"; // ?
            
            // Lowercase u with diacritics
            map['\u00F9'] = "u"; // ù
            map['\u00FA'] = "u"; // ú
            map['\u1EE7'] = "u"; // ?
            map['\u0169'] = "u"; // ?
            map['\u1EE5'] = "u"; // ?
            map['\u01B0'] = "u"; // ?
            map['\u1EEB'] = "u"; // ?
            map['\u1EE9'] = "u"; // ?
            map['\u1EED'] = "u"; // ?
            map['\u1EEF'] = "u"; // ?
            map['\u1EF1'] = "u"; // ?
            
            // Lowercase y with diacritics
            map['\u1EF3'] = "y"; // ?
            map['\u00FD'] = "y"; // ý
            map['\u1EF7'] = "y"; // ?
            map['\u1EF9'] = "y"; // ?
            map['\u1EF5'] = "y"; // ?
            
            // Uppercase D with stroke (?) - U+0110
            map['\u0110'] = "d";
            
            // Uppercase A with diacritics
            map['\u00C0'] = "a"; // À
            map['\u00C1'] = "a"; // Á
            map['\u1EA2'] = "a"; // ?
            map['\u00C3'] = "a"; // Ã
            map['\u1EA0'] = "a"; // ?
            map['\u0102'] = "a"; // ?
            map['\u1EB0'] = "a"; // ?
            map['\u1EAE'] = "a"; // ?
            map['\u1EB2'] = "a"; // ?
            map['\u1EB4'] = "a"; // ?
            map['\u1EB6'] = "a"; // ?
            map['\u00C2'] = "a"; // Â
            map['\u1EA6'] = "a"; // ?
            map['\u1EA4'] = "a"; // ?
            map['\u1EA8'] = "a"; // ?
            map['\u1EAA'] = "a"; // ?
            map['\u1EAC'] = "a"; // ?
            
            // Uppercase E with diacritics
            map['\u00C8'] = "e"; // È
            map['\u00C9'] = "e"; // É
            map['\u1EBA'] = "e"; // ?
            map['\u1EBC'] = "e"; // ?
            map['\u1EB8'] = "e"; // ?
            map['\u00CA'] = "e"; // Ê
            map['\u1EC0'] = "e"; // ?
            map['\u1EBE'] = "e"; // ?
            map['\u1EC2'] = "e"; // ?
            map['\u1EC4'] = "e"; // ?
            map['\u1EC6'] = "e"; // ?
            
            // Uppercase I with diacritics
            map['\u00CC'] = "i"; // Ì
            map['\u00CD'] = "i"; // Í
            map['\u1EC8'] = "i"; // ?
            map['\u0128'] = "i"; // ?
            map['\u1ECA'] = "i"; // ?
            
            // Uppercase O with diacritics
            map['\u00D2'] = "o"; // Ò
            map['\u00D3'] = "o"; // Ó
            map['\u1ECE'] = "o"; // ?
            map['\u00D5'] = "o"; // Õ
            map['\u1ECC'] = "o"; // ?
            map['\u00D4'] = "o"; // Ô
            map['\u1ED2'] = "o"; // ?
            map['\u1ED0'] = "o"; // ?
            map['\u1ED4'] = "o"; // ?
            map['\u1ED6'] = "o"; // ?
            map['\u1ED8'] = "o"; // ?
            map['\u01A0'] = "o"; // ?
            map['\u1EDC'] = "o"; // ?
            map['\u1EDA'] = "o"; // ?
            map['\u1EDE'] = "o"; // ?
            map['\u1EE0'] = "o"; // ?
            map['\u1EE2'] = "o"; // ?
            
            // Uppercase U with diacritics
            map['\u00D9'] = "u"; // Ù
            map['\u00DA'] = "u"; // Ú
            map['\u1EE6'] = "u"; // ?
            map['\u0168'] = "u"; // ?
            map['\u1EE4'] = "u"; // ?
            map['\u01AF'] = "u"; // ?
            map['\u1EEA'] = "u"; // ?
            map['\u1EE8'] = "u"; // ?
            map['\u1EEC'] = "u"; // ?
            map['\u1EEE'] = "u"; // ?
            map['\u1EF0'] = "u"; // ?
            
            // Uppercase Y with diacritics
            map['\u1EF2'] = "y"; // ?
            map['\u00DD'] = "y"; // Ý
            map['\u1EF6'] = "y"; // ?
            map['\u1EF8'] = "y"; // ?
            map['\u1EF4'] = "y"; // ?
            
            return map;
        }

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

            // Convert Vietnamese characters first
            var sb = new StringBuilder(title.Length);
            foreach (var c in title)
            {
                if (VietnameseCharMap.TryGetValue(c, out var replacement))
                {
                    sb.Append(replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }
            title = sb.ToString();

            // Convert to lowercase
            title = title.ToLowerInvariant();

            // Normalize the string (decompose characters)
            title = title.Normalize(NormalizationForm.FormD);
            
            // Remove diacritical marks (accents)
            sb.Clear();
            foreach (var c in title)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            title = sb.ToString().Normalize(NormalizationForm.FormC);

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
        /// Generates a unique slug by appending an ID
        /// </summary>
        /// <param name="title">The title to convert</param>
        /// <param name="id">The unique identifier to append</param>
        /// <returns>A URL-friendly slug with ID suffix</returns>
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
