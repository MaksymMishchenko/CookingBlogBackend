using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Controllers.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;
using PostApiService.Models.Enums;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidateIdAttributeTests
    {
        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenIdIsLessThanOrEqualToZero_ForPost()
        {
            // Arrange
            var context = CreateContextForId(0);
            var attribute = new ValidateIdAttribute
            {
                InvalidIdErrorMessage = PostErrorMessages.InvalidPostIdParameter
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.Equal(PostErrorMessages.InvalidPostIdParameter, response.Message);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenIdIsLessThanOrEqualToZero_ForComment()
        {
            // Arrange
            var context = CreateContextForId(0);
            var attribute = new ValidateIdAttribute
            {
                InvalidIdErrorMessage = CommentErrorMessages.InvalidCommentIdParameter
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.Equal(CommentErrorMessages.InvalidCommentIdParameter, response.Message);
        }

        [Fact]
        public void OnActionExecuting_ShouldNotReturnBadRequest_WhenIdIsGreaterThanZero()
        {
            // Arrange
            var context = CreateContextForId(1);
            var attribute = new ValidateIdAttribute
            {
                InvalidIdErrorMessage = "Invalid ID.",
                ErrorResponseType = ResourceType.Post
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        private ActionExecutingContext CreateContextForId(int id)
        {
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            };

            var actionArguments = new Dictionary<string, object>
            {
                { "id", id }
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

