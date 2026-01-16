using Microsoft.Extensions.Options;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Services;

namespace PostApiService.Tests.Services
{
    public class HtmlSanitizationServiceTests
    {
        private readonly IOptions<SanitizerConfiguration> _optionsMock;
        private readonly SanitizerConfiguration _config;

        public HtmlSanitizationServiceTests()
        {
            _optionsMock = Substitute.For<IOptions<SanitizerConfiguration>>();

            _config = new SanitizerConfiguration
            {
                Comment = new SanitizerConfiguration.RuleSet
                {
                    AllowedTags = new List<string> { "b", "i", "u", "strong", "em", "p", "br" },
                    AllowedAttributes = new List<string>()
                },
                Post = new SanitizerConfiguration.RuleSet
                {
                    AllowedTags = new List<string> { "b", "p", "h1", "img", "blockquote" },
                    AllowedAttributes = new List<string> { "class", "src" }
                }
            };

            _optionsMock.Value.Returns(_config);
        }

        [Fact]
        public void SanitizePost_ShouldAllowImagesAndHeadings()
        {
            // Arrange
            var service = new HtmlSanitizationService(_optionsMock);
            string input = "<h1>Title</h1><p>Text</p><img src='image.jpg' alt='test' />";           
            string expected = "<h1>Title</h1><p>Text</p><img src=\"image.jpg\">";

            // Act
            string result = service.SanitizePost(input);

            // Assert
            Assert.Equal(expected, result);
        }        

        [Fact]
        public void SanitizePost_ShouldRemoveTagsAllowedInPostButForbiddenInComment()
        {
            // Arrange
            var service = new HtmlSanitizationService(_optionsMock);
            string input = "<blockquote>Quote</blockquote><img src='x.jpg'>";

            // Act & Assert            
            var postResult = service.SanitizePost(input);
            Assert.Contains("<blockquote>", postResult);
            Assert.Contains("<img", postResult);
            
            var commentResult = service.SanitizeComment(input);
            Assert.DoesNotContain("<blockquote>", commentResult);
            Assert.DoesNotContain("<img", commentResult);
        }

        [Fact]
        public void SanitizeComment_ShouldKeepAllowedTagsAndRemoveForbiddenOnes()
        {
            // Arrange
            var service = new HtmlSanitizationService(_optionsMock);
            string input = "<p>Hello <b>World</b></p><div>Invisible</div>";
            string expected = "<p>Hello <b>World</b></p>";

            // Act
            string result = service.SanitizeComment(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SanitizeComment_ShouldRemoveScriptsButKeepTextInAllowedTags()
        {
            // Arrange
            var service = new HtmlSanitizationService(_optionsMock);
            string input = "<p>Safe text<script>alert('xss')</script></p>";
            string expected = "<p>Safe text</p>";

            // Act
            string result = service.SanitizeComment(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SanitizeComment_ShouldRemoveAllAttributes_WhenAllowedAttributesIsEmpty()
        {
            // Arrange
            var service = new HtmlSanitizationService(_optionsMock);
            string input = "<p title='hint' style='color:red'>Text with attributes</p>";
            string expected = "<p>Text with attributes</p>";

            // Act
            string result = service.SanitizeComment(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void SanitizeComment_ShouldHandleNullAndEmpty(string input, string expected)
        {
            // Arrange
            var service = new HtmlSanitizationService(_optionsMock);

            // Act
            string result = service.SanitizeComment(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SanitizeComment_ShouldNormalizeClosedTags()
        {
            // Arrange
            var service = new HtmlSanitizationService(_optionsMock);
            string input = "<b>Bold text";
            string expected = "<b>Bold text</b>";

            // Act
            string result = service.SanitizeComment(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}