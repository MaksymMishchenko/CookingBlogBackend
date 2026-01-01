using HtmlAgilityPack;
using PostApiService.Interfaces;
using System.Net;
using System.Text.RegularExpressions;

namespace PostApiService.Services
{
    public class SnippetGeneratorService : ISnippetGeneratorService
    {
        private string GetStartOfText(string text, int length)
        {
            if (text.Length <= length) return text;
            return text.Substring(0, length).Trim() + "...";
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            string result = htmlDoc.DocumentNode.InnerText;
            return WebUtility.HtmlDecode(result);
        }

        public string CreateSnippet(string content, string searchKeyword, int contextLength = 200)
        {
            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(searchKeyword))
            {
                return string.Empty;
            }

            string plainText = StripHtml(content);

            var keywordIndex = plainText.IndexOf(searchKeyword, StringComparison.OrdinalIgnoreCase);

            if (keywordIndex == -1)
            {
                return GetStartOfText(plainText, contextLength);
            }

            int halfContext = contextLength / 2;

            var start = Math.Max(0, keywordIndex - halfContext);
            var end = Math.Min(plainText.Length, keywordIndex + searchKeyword.Length + halfContext);

            if (start == 0 && end < plainText.Length)
            {
                end = Math.Min(plainText.Length, end + (halfContext - keywordIndex));
            }

            if (end == plainText.Length && start > 0)
            {
                start = Math.Max(0, start - (halfContext - (plainText.Length - (keywordIndex + searchKeyword.Length))));
            }

            var snippet = plainText.Substring(start, end - start);

            var prefix = start > 0 ? "..." : "";
            var suffix = end < plainText.Length ? "..." : "";

            var highlightedSnippet = Regex.Replace(
                snippet,
                Regex.Escape(searchKeyword),
                match => $"<b>{match.Value}</b>",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(100)
            );

            return prefix + highlightedSnippet + suffix;
        }
    }
}
