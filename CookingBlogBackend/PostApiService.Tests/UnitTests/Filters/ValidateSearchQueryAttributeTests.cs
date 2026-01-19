using PostApiService.Controllers.Filters.PostApiService.Controllers.Filters;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidateSearchQueryAttributeTests : FilterTestBase
    {
        private readonly ValidateSearchQueryAttribute _filter;

        public ValidateSearchQueryAttributeTests()
        {
            _filter = new ValidateSearchQueryAttribute();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void OnActionExecuting_ShouldReturnError_WhenQueryIsWhitespace(string? query)
        {
            // Arrange
            var searchParams = new SearchPostQueryParameters { QueryString = query! };
            var context = CreateContext(new Dictionary<string, object?> { { "query", searchParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.SearchQueryRequired, response.Errors![nameof(searchParams.QueryString)][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnError_WhenQueryIsTooShort()
        {
            // Arrange
            var searchParams = new SearchPostQueryParameters { QueryString = "ab" };
            var context = CreateContext(new Dictionary<string, object?> { { "query", searchParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            var expected = string.Format(Global.Validation.SearchQueryTooShort, _filter.MinQueryLength);
            Assert.Equal(expected, response.Errors![nameof(searchParams.QueryString)][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnError_WhenQueryHasForbiddenCharacters()
        {
            // Arrange
            var searchParams = new SearchPostQueryParameters { QueryString = "search<script>" };
            var context = CreateContext(new Dictionary<string, object?> { { "query", searchParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.SearchQueryForbiddenCharacters, response.Errors![nameof(searchParams.QueryString)][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnError_WhenQueryHasNoLettersOrDigits()
        {
            // Arrange
            var searchParams = new SearchPostQueryParameters { QueryString = "---..." };
            var context = CreateContext(new Dictionary<string, object?> { { "query", searchParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.SearchQueryMustContainLetterOrDigit, response.Errors![nameof(searchParams.QueryString)][0]);
        }

        [Theory]
        [InlineData("DotNet")]
        [InlineData("asp-core")]
        [InlineData("version 8.0")]
        public void OnActionExecuting_ShouldAllowValidSearchQueries(string validQuery)
        {
            // Arrange
            var searchParams = new SearchPostQueryParameters { QueryString = validQuery };
            var context = CreateContext(new Dictionary<string, object?> { { "query", searchParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }
    }
}