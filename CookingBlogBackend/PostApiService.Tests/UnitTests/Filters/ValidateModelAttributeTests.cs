using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidateModelAttributeTests : FilterTestBase
    {
        private readonly ValidateModelAttribute _filter;

        public ValidateModelAttributeTests()
        {
            _filter = new ValidateModelAttribute();
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenRequestBodyIsMissing()
        {
            // Arrange
            var actionArguments = new Dictionary<string, object?>();
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(ResponseErrorMessages.RequestBodyRequired, response.Message);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var body = new { Title = "" };
            var actionArguments = new Dictionary<string, object?> { { "dto", body } };
            var context = CreateContext(actionArguments);

            const string propertyName = "Title";
            context.ModelState.AddModelError(propertyName, "Title is required");

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.Equal(ResponseErrorMessages.ValidationFailed, response.Message);
            Assert.NotNull(response.Errors);
            Assert.True(response.Errors.ContainsKey(propertyName));
        }

        [Fact]
        public void OnActionExecuting_ShouldDoNothing_WhenBodyIsPresentAndModelIsValid()
        {
            // Arrange:
            var body = new { Title = "Valid Title", Content = "Some valid content" };
            var actionArguments = new Dictionary<string, object?> { { "dto", body } };
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }
    }
}