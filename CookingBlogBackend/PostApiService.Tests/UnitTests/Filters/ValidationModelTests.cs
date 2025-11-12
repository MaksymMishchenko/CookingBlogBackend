using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
using PostApiService.Models;
using PostApiService.Models.Enums;
using Microsoft.AspNetCore.Routing;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidationModelTests
    {
        private ActionExecutingContext GetActionExecutingContext(bool isModelValid, Dictionary<string, string[]> modelErrors = null)
        {
            var modelState = new ModelStateDictionary();

            if (!isModelValid && modelErrors != null)
            {
                foreach (var error in modelErrors)
                {
                    foreach (var msg in error.Value)
                    {
                        modelState.AddModelError(error.Key, msg);
                    }
                }
            }

            var actionContext = new ActionContext
            {
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor(),
                HttpContext = new DefaultHttpContext()
            };

            actionContext.ModelState.Merge(modelState);

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                controller: null
            );
        }

        [Fact]
        public void OnActionExecuting_ModelStateInvalid_ReturnsBadRequestWithPostError()
        {
            // Arrange
            var attribute = new ValidateModelAttribute
            {
                ErrorResponseType = ResourceType.Post,
                InvalidErrorMessage = "Invalid post data"
            };

            var errors = new Dictionary<string, string[]>
        {
            { "Title", new[] { "Title is required" } }
        };

            var context = GetActionExecutingContext(false, errors);

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var apiResponse = Assert.IsType<ApiResponse<Post>>(result.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Invalid post data", apiResponse.Message);
        }

        [Fact]
        public void OnActionExecuting_ModelStateValid_DoesNotSetResult()
        {
            // Arrange
            var attribute = new ValidateModelAttribute
            {
                ErrorResponseType = ResourceType.Comment,
                InvalidErrorMessage = "Invalid comment data"
            };

            var context = GetActionExecutingContext(true);

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_ModelStateInvalid_ReturnsLoginUserError()
        {
            // Arrange
            var attribute = new ValidateModelAttribute
            {
                ErrorResponseType = ResourceType.LoginUser,
                InvalidErrorMessage = "Invalid login data"
            };

            var errors = new Dictionary<string, string[]>
        {
            { "Password", new[] { "Password is required" } }
        };

            var context = GetActionExecutingContext(false, errors);

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var apiResponse = Assert.IsType<ApiResponse<LoginUser>>(result.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Invalid login data", apiResponse.Message);
        }
    }
}

