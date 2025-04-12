using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Controllers.Filters;
using PostApiService.Models;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidatePostIdMatchAttributeTests
    {
        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenPostIdMismatch()
        {
            // Arrange
            var context = CreateContextForPostIdAndComment(1, new Comment { PostId = 2 });
            var attribute = new ValidatePostIdMatchAttribute
            {
                InvalidPostIdErrorMessage = "PostId mismatch!"
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse<Comment>>(badRequestResult.Value);
            Assert.Equal("PostId mismatch!", response.Message);
        }

        [Fact]
        public void OnActionExecuting_ShouldNotReturnBadRequest_WhenPostIdMatches()
        {
            // Arrange
            var context = CreateContextForPostIdAndComment(1, new Comment { PostId = 1 });
            var attribute = new ValidatePostIdMatchAttribute
            {
                InvalidPostIdErrorMessage = "PostId mismatch!"
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_ShouldNotReturnBadRequest_WhenCommentIsNull()
        {
            // Arrange
            var context = CreateContextForPostIdAndComment(1, null);
            var attribute = new ValidatePostIdMatchAttribute
            {
                InvalidPostIdErrorMessage = "PostId mismatch!"
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        private ActionExecutingContext CreateContextForPostIdAndComment(int postId, Comment comment)
        {
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            };

            var actionArguments = new Dictionary<string, object>
            {
                { "postId", postId },
                { "comment", comment }
            };

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null
            );
        }
    }
}
