using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class SnippetGeneratorServiceTests
    {
        private readonly SnippetGeneratorService _snippetGen;
        public SnippetGeneratorServiceTests()
        {
            _snippetGen = new SnippetGeneratorService();
        }

        [Theory]
        [InlineData("", "query")]
        [InlineData("content", "")]
        [InlineData(null, "query")]
        [InlineData("content", null)]
        public void CreateSnippet_ShouldReturnEmpty_WhenInputIsInvalid(string content, string keyword)
        {
            // Act
            var result = _snippetGen.CreateSnippet(content, keyword);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void CreateSnippet_ShouldReturnStartOfText_WhenKeywordNotFound()
        {
            // Arrange
            var content = "This is some random text.";
            var keyword = "nonexistent";
            var expected = "This is some random text.";

            // Act
            var result = _snippetGen.CreateSnippet(content, keyword);

            // Assert            
            Assert.Equal(expected, result);                       
        }

        [Fact]
        public void CreateSnippet_ShouldHighlightKeyword_RegardlessOfCase()
        {
            // Arrange
            var content = "This recipe requires fresh CHILI for the best flavor.";
            var keyword = "chili";

            // Act
            var result = _snippetGen.CreateSnippet(content, keyword);

            // Assert            
            Assert.Contains("<b>CHILI</b>", result);
            Assert.DoesNotContain("<b>chili</b>", result);
        }

        [Fact]
        public void CreateSnippet_ShouldAddEllipsis_WhenKeywordInMiddle()
        {
            // Arrange
            var content = "For the best results, the chicken should be marinated in a mixture of lemon juice," +
                " olive oil, and chili for at least four hours.";
            var keyword = "marinated";
            int contextLength = 20;

            // Act
            var result = _snippetGen.CreateSnippet(content, keyword, contextLength);

            // Assert            
            Assert.StartsWith("...", result);
            Assert.EndsWith("...", result);
            Assert.Contains("<b>marinated</b>", result);
        }

        [Fact]
        public void CreateSnippet_ShouldNotAddStartEllipsis_WhenKeywordIsAtBeginning()
        {
            // Arrange
            var content = "Chili powder should be added gradually to control the heat level of your sauce";
            var keyword = "Chili";
            int contextLength = 10;

            // Act
            var result = _snippetGen.CreateSnippet(content, keyword, contextLength);

            // Assert
            Assert.False(result.StartsWith("..."),
                "Snippet should not start with ellipsis when the keyword is at the very beginning.");
            Assert.EndsWith("...", result);
            Assert.Contains("<b>Chili</b>", result);
        }

        [Fact]
        public void CreateSnippet_ShouldNotAddEndEllipsis_WhenKeywordIsAtEnd()
        {
            // Arrange
            var content = "To make the wings extra spicy, you can add a few drops of Tabasco";
            var keyword = "Tabasco";
            int contextLength = 10;

            // Act
            var result = _snippetGen.CreateSnippet(content, keyword, contextLength);

            // Assert
            Assert.StartsWith("...", result);
            Assert.False(result.EndsWith("..."),
                "Snippet should not end with ellipsis when the keyword is at the very end of the content.");
            Assert.Contains("<b>Tabasco</b>", result);
        }

        [Fact]
        public void CreateSnippet_ShouldNotAddAnyEllipsis_WhenContentIsShort()
        {
            // Arrange
            var content = "Fresh tomato soup.";
            var keyword = "tomato";
            int contextLength = 100;

            // Act
            var result = _snippetGen.CreateSnippet(content, keyword, contextLength);

            // Assert
            Assert.Equal("Fresh <b>tomato</b> soup.", result);
            Assert.DoesNotContain(result, "...");
        }

        [Fact]
        public void CreateSnippet_ShouldStripHtmlTags_BeforeProcessing()
        {
            // Arrange
            var content = "<p>Add some <strong>fresh</strong> <a href='/ingredients/pepper'>pepper</a> to the sauce.</p>";
            var keyword = "pepper";

            // Act
            var result = _snippetGen.CreateSnippet(content, keyword, 50);

            // Assert
            Assert.DoesNotContain("<p>", result);
            Assert.DoesNotContain("<strong>", result);
            Assert.DoesNotContain("<a", result);
            Assert.Contains("Add some fresh <b>pepper</b> to the sauce.", result);
        }
    }
}
