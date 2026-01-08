using Microsoft.AspNetCore.Routing;
using PostApiService.Extensions;
using PostApiService.Infrastructure.Common;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Extensions
{
    public class ResultExtensionsTests
    {
        [Fact]
        public void ToActionResult_Generic_SimpleObject_ReturnsOkWithData()
        {
            // Arrange
            var data = new { Title = "Simple Post" };
            var result = Result<object>.Success(data, "Success Message");

            // Act
            var actionResult = result.ToActionResult();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Success Message", response.Message);
            Assert.Equal(data, response.Data);
        }

        [Fact]
        public void ToActionResult_NoContent_ReturnsNoContentResult()
        {
            // Arrange
            var result = Result.NoContent();

            // Act
            var actionResult = result.ToActionResult();

            // Assert
            Assert.IsType<NoContentResult>(actionResult);
        }
        
        private class ResultTestDouble : Result
        {
            public ResultTestDouble(ResultStatus status, string msg, string code)
                : base(status, msg, code) { }
        }

        [Theory]
        [InlineData(ResultStatus.NotFound, 404, typeof(NotFoundObjectResult))]
        [InlineData(ResultStatus.Conflict, 409, typeof(ConflictObjectResult))]
        [InlineData(ResultStatus.Invalid, 400, typeof(BadRequestObjectResult))]
        [InlineData(ResultStatus.Unauthorized, 401, typeof(UnauthorizedObjectResult))]
        [InlineData(ResultStatus.Forbidden, 403, typeof(ObjectResult))]
        [InlineData(ResultStatus.Error, 500, typeof(ObjectResult))]
        public void ToActionResult_ShouldMapErrorStatusesToCorrectHttpResults(
            ResultStatus status,
            int expectedStatusCode,
            Type expectedType)
        {
            // Arrange
            var message = "Test error message";
            var errorCode = "TEST_CODE";
           
            var result = new ResultTestDouble(status, message, errorCode);

            // Act
            var actionResult = result.ToActionResult();

            // Assert
            Assert.IsType(expectedType, actionResult);

            var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult);

            Assert.Equal(expectedStatusCode, objectResult.StatusCode);

            var apiResponse = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal(message, apiResponse.Message);
            Assert.Equal(errorCode, apiResponse.ErrorCode);           
        }

        [Fact]
        public void ToActionResult_ShouldReturnPaginatedResponse_WhenValueIsIPagedResult()
        {
            // Arrange
            var items = new List<string> { "Post 1", "Post 2" };
            var pagedData = Substitute.For<IPagedResult>();
            pagedData.GetItems().Returns(items);
            pagedData.PageNumber.Returns(1);
            pagedData.PageSize.Returns(10);
            pagedData.TotalCount.Returns(50);

            var result = Result<IPagedResult>.Success(pagedData);

            // Act
            var actionResult = result.ToActionResult();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.Equal(items.Count, response.Data.Count());
        }

        [Fact]
        public void ToActionResult_SearchPagedResult_MapsAllFieldsCorrectly()
        {
            // Arrange
            var items = new List<SearchPostListDto> { new(1, "Title", "slug", "snippet", "author", "cat") };
            var searchRecord = new PagedSearchResult<SearchPostListDto>(
                Query: "dessert",
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

            Assert.True(response.Success);
            Assert.Equal("dessert", response.SearchQuery);
            Assert.Equal("Found 1 posts", response.Message);
            Assert.Equal(1, response.TotalCount);
            
            var data = Assert.IsAssignableFrom<IEnumerable<object>>(response.Data);
            Assert.Single(data);
            var firstItem = Assert.IsType<SearchPostListDto>(data.First());
            Assert.Equal(1, firstItem.Id);
        }                       

        [Fact]
        public void ToCreatedResult_ShouldReturnErrorResult_WhenResultFails()
        {
            // Arrange
            var result = Result<object>.Conflict("Slug already exists");

            // Act
            var actionResult = result.ToCreatedResult("GetPost", new { id = 1 });

            // Assert          
            Assert.IsType<ConflictObjectResult>(actionResult);
        }

        [Fact]
        public void ToCreatedResult_ShouldReturnCreatedAtAction_WhenSuccess()
        {
            // Arrange
            var data = new { Id = 1, Title = "New Post" };
            var result = Result<object>.Success(data, "Created successfully");
            var actionName = "GetPost";
            var routeValues = new { id = 1 };

            // Act
            var actionResult = result.ToCreatedResult(actionName, routeValues);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
            Assert.Equal(actionName, createdResult.ActionName);           
            var routes = new RouteValueDictionary(createdResult.RouteValues);
            Assert.Equal(1, routes["id"]);

            var response = Assert.IsType<ApiResponse<object>>(createdResult.Value);
            Assert.Equal(data, response.Data);
            Assert.Equal("Created successfully", response.Message);
        }        
    }
}
