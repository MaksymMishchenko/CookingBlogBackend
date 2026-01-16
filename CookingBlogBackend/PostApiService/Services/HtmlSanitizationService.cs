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

            sanitizer.RemovingAttribute += (s, e) =>
            {
                if (e.Attribute.Name == "class" && e.Attribute.Value.StartsWith("ql-"))
                {
                    e.Cancel = true;
                }
            };
            return sanitizer;
        }

        public string SanitizeComment(string html) => _commentSanitizer.Sanitize(html);
        public string SanitizePost(string html) => _postSanitizer.Sanitize(html);
    }
}
