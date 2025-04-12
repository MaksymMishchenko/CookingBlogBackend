using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
using PostApiService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                InvalidIdErrorMessage = "Invalid post ID.",
                ErrorResponseType = ResourceType.Post
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);
            Assert.Equal("Invalid post ID.", response.Message);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenIdIsLessThanOrEqualToZero_ForComment()
        {
            // Arrange
            var context = CreateContextForId(0);
            var attribute = new ValidateIdAttribute
            {
                InvalidIdErrorMessage = "Invalid comment ID.",
                ErrorResponseType = ResourceType.Comment
            };

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse<Comment>>(badRequestResult.Value);
            Assert.Equal("Invalid comment ID.", response.Message);
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

