using PostApiService.Extensions;
using PostApiService.Infrastructure.Common;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Extensions
{
    public class ResultExtensionsTests
    {
        [Fact]
        public void ToActionResult_SearchPagedResult_ReturnsCorrectFields()
        {
            // Arrange
            var items = new List<SearchPostListDto> { new(1, "Title", "slug", "snippet", "author", "cat") };
            var searchRecord = new PagedSearchResult<SearchPostListDto>(
                Query: "dotnet",
                Items: items,
                TotalSearchCount: 1,
                PageNumber: 1,
                PageSize: 10,
                Message: "Found 1 posts"
            );
            var result = Result<PagedSearchResult<SearchPostListDto>>.Success(searchRecord);

            // Act
            var actionResult = result.ToActionResult();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.Equal("dotnet", response.SearchQuery);
            Assert.Equal("Found 1 posts", response.Message);
            Assert.Single(response.Data);
        }

        [Theory]
        [InlineData(ResultStatus.NotFound, typeof(NotFoundObjectResult))]
        [InlineData(ResultStatus.Conflict, typeof(ConflictObjectResult))]
        [InlineData(ResultStatus.Invalid, typeof(BadRequestObjectResult))]
        public void ToActionResult_Errors_ReturnCorrectStatusCodes(ResultStatus status, Type expectedType)
        {
            // Arrange
            var errorMsg = "Some error occurred";
            
            var result = status switch
            {
                ResultStatus.NotFound => Result<object>.NotFound(errorMsg),
                ResultStatus.Conflict => Result<object>.Conflict(errorMsg),
                _ => Result<object>.Invalid(errorMsg)
            };

            // Act
            var actionResult = result.ToActionResult();

            // Assert
            Assert.IsType(expectedType, actionResult);
            var objectResult = actionResult as ObjectResult;
            var apiResponse = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.Equal(errorMsg, apiResponse.Message);
            Assert.False(apiResponse.Success);
        }

        [Fact]
        public void ToActionResult_NoContentWithMessage_ReturnsOkWithBody()
        {
            // Arrange
            var result = Result<object>.NoContent();
            var msg = "Successfully deleted";

            // Act
            var actionResult = result.ToActionResult(msg);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.Equal(msg, response.Message);
            Assert.True(response.Success);
        }
    }
}
