using System.Text.RegularExpressions;

namespace PostApiService.Helper
{
    public static class HtmlContentImageExtractor
    {
        private static readonly Regex ImgSrcRegex = new(
            @"<img\b[^>]*?\bsrc\s*=\s*[""'](?<src>[^""']+)[""'][^>]*?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

        public static IReadOnlyList<string> ExtractImageUrls(string? htmlContent, string? allowedHostOrPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return [];
            }

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in ImgSrcRegex.Matches(htmlContent))
            {
                if (!match.Success)
                {
                    continue;
                }

                var rawUrl = match.Groups["src"].Value.Trim();
                if (string.IsNullOrWhiteSpace(rawUrl))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(allowedHostOrPrefix) &&
                    !rawUrl.StartsWith(allowedHostOrPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (seen.Add(rawUrl))
                {
                    result.Add(rawUrl);
                }
            }

            return result;
        }
    }
}
