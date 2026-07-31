using Ganss.Xss;
using Microsoft.Extensions.Options;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Interfaces;

namespace PostApiService.Services
{
    public class HtmlSanitizationService : IHtmlSanitizationService
    {
        private readonly HtmlSanitizer _postSanitizer;
        private readonly HtmlSanitizer _commentSanitizer;

        public HtmlSanitizationService(IOptions<SanitizerConfiguration> options)
        {
            var settings = options.Value;

            _commentSanitizer = CreateSanitizer(settings.Comment);
            _postSanitizer = CreatePostSanitizer(settings.Post);
        }

        private static HtmlSanitizer CreateSanitizer(SanitizerConfiguration.RuleSet rules)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith(rules.AllowedTags);

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.UnionWith(rules.AllowedAttributes);

            return sanitizer;
        }

        private static HtmlSanitizer CreatePostSanitizer(SanitizerConfiguration.RuleSet rules)
        {
            var sanitizer = CreateSanitizer(rules);

            sanitizer.AllowedTags.UnionWith(new[]
            {
                "h1", "h2", "h3", "h4", "h5", "h6",
                "p", "span", "strong", "em", "u", "s",
                "blockquote", "pre", "ol", "ul", "li", "a", "img", "br"
            });

            sanitizer.AllowedAttributes.UnionWith(new[]
            {
                "class", "style", "href", "target", "rel", "src", "alt"
            });

            sanitizer.AllowedCssProperties.UnionWith(new[]
            {
                "color", "background-color", "text-align"
            });

            return sanitizer;
        }

        private static string NormalizeHtml(string html)
        {
            return html.Replace("&nbsp;", " ");
        }

        public string SanitizeComment(string html) => _commentSanitizer.Sanitize(html);

        public string SanitizePost(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }
            
            var sanitizedHtml = _postSanitizer.Sanitize(html);
            
            return NormalizeHtml(sanitizedHtml);
        }
    }
}
