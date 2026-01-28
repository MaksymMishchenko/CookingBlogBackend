using AnyAscii;
using System.Text.RegularExpressions;

namespace PostApiService.Helper
{
    public static class StringHelper
    {
        public static string Truncate(this string? value, int maxLength = 500)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }

        public static string StripHtml(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return Regex.Replace(input, "<.*?>", string.Empty).Trim();
        }

        public static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
           
            string slug = name.Transliterate();
            
            slug = slug.ToLowerInvariant();
           
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            
            slug = Regex.Replace(slug, @"\s+", "-").Trim();
           
            slug = Regex.Replace(slug, @"-+", "-");

            return slug.Trim('-');
        }
    }
}
