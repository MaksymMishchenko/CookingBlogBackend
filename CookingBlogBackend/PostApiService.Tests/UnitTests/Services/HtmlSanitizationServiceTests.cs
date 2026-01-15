using Microsoft.Extensions.Options;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class HtmlSanitizationServiceTests
    {
        private readonly SanitizerConfiguration _testConfig;

        public HtmlSanitizationServiceTests()
        {            
            _testConfig = new SanitizerConfiguration
            {
                Comment = new SanitizerConfiguration.RuleSet
                {
                    AllowedTags = new List<string> { "b", "i", "p" },
                    AllowedAttributes = new List<string> { "class" }
                }
            };
        }

        [Fact]
        public void SanitizeComment_ShouldKeepAllowedTagsAndAttributes()
        {
            // Arrange
            var options = Options.Create(_testConfig);
            var service = new HtmlSanitizationService(options);
            var input = "<p>Hello, <b class=\"bold-text\">Bob</b>!</p>";

            // Act
            var result = service.SanitizeComment(input);

            // Assert
            Assert.Contains("<p>", result);
            Assert.Contains("<b class=\"bold-text\">", result);
            Assert.Contains("</b>", result);
        }

        [Fact]
        public void SanitizeComment_ShouldRemoveDangerousTags()
        {
            // Arrange
            var options = Options.Create(_testConfig);
            var service = new HtmlSanitizationService(options);
            var input = "Text <script>alert('xss')</script> <iframe src='bad-site.com'></iframe>";

            // Act
            var result = service.SanitizeComment(input);

            // Assert
            Assert.DoesNotContain("<script>", result);
            Assert.DoesNotContain("<iframe>", result);           
            Assert.Contains("Text", result);
        }

        [Fact]
        public void SanitizeComment_ShouldRemoveDisallowedAttributes()
        {
            // Arrange
            var options = Options.Create(_testConfig);
            var service = new HtmlSanitizationService(options);
            var input = "<i style=\"color: red\" onclick=\"hack()\">Italic</i>";

            // Act
            var result = service.SanitizeComment(input);

            // Assert
            Assert.Contains("<i>", result);
            Assert.DoesNotContain("style=", result);
            Assert.DoesNotContain("onclick=", result);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void SanitizeComment_ShouldHandleNullOrEmpty(string input, string expected)
        {
            // Arrange
            var options = Options.Create(_testConfig);
            var service = new HtmlSanitizationService(options);

            // Act
            var result = service.SanitizeComment(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
