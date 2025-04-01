using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using PostApiService.Controllers.Filters;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidationModelTests
    {
        [Fact]
        public void ValidateModelAttribute_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Title", "Title is required");

            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new Mock<ControllerBase>().Object
            );

            var filter = new ValidateModelAttribute();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.IsType<BadRequestObjectResult>(context.Result);
        }
    }
}
