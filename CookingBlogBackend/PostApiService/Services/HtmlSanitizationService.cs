using Ganss.Xss;
using Microsoft.Extensions.Options;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Interfaces;

namespace PostApiService.Services
{
    public class HtmlSanitizationService : IHtmlSanitizationService
    {
        private readonly HtmlSanitizer _commentSanitizer;

        public HtmlSanitizationService(IOptions<SanitizerConfiguration> options)
        {
            var settings = options.Value;

            _commentSanitizer = CreateSanitizer(settings.Comment);
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

        public string SanitizeComment(string html) => _commentSanitizer.Sanitize(html);
    }
}
