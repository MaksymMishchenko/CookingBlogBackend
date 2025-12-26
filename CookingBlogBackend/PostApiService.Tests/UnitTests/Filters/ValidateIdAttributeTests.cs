using PostApiService.Controllers.Filters;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidateIdAttributeTests : FilterTestBase
    {
        private readonly ValidateIdAttribute _filter;

        public ValidateIdAttributeTests()
        {
            _filter = new ValidateIdAttribute();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void OnActionExecuting_ShouldSetResultToBadRequest_WhenIdIsInvalid(int invalidId)
        {
            // Arrange
            var argumentKey = "postId";
            var actionArguments = new Dictionary<string, object?> { { "postId", invalidId } };
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);

            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(Global.Validation.ValidationFailed, response.Message);

            Assert.NotNull(response.Errors);
            Assert.True(response.Errors.ContainsKey(argumentKey));
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var context = CreateContext(new Dictionary<string, object?>());
            const string propertyName = "someProperty";
            const string errorMessage = "Invalid format";
            context.ModelState.AddModelError(propertyName, errorMessage);

            // Act
            _filter.OnActionExecuting(context);

            // Assert            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);

            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(Global.Validation.ValidationFailed, response.Message);

            Assert.NotNull(response.Errors);
            Assert.True(response.Errors.ContainsKey(propertyName));
        }

        [Fact]
        public void OnActionExecuting_ShouldDoNothing_WhenIdIsValid()
        {
            // Arrange
            var actionArguments = new Dictionary<string, object?> { { "id", 10 } };
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert            
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_ShouldIgnoreArgument_WhenKeyContainsIdButValueIsNotInt()
        {
            // Arrange
            var actionArguments = new Dictionary<string, object?> { { "correlationId", "not-an-int" } };
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }
    }
}

