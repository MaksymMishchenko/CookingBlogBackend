using PostApiService.Controllers.Filters;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidatePaginationParametersTests : FilterTestBase
    {
        private readonly ValidatePaginationParametersAttribute _filter;

        public ValidatePaginationParametersTests()
        {
            _filter = new ValidatePaginationParametersAttribute { MaxPageSize = 10 };
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 0)]
        [InlineData(-1, -1)]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenParametersAreLessThanOne(int pageNumber, int pageSize)
        {
            // Arrange
            var query = new PaginationQueryParameters { PageNumber = pageNumber, PageSize = pageSize };
            var actionArguments = new Dictionary<string, object?> { { "query", query } };
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.Equal(Global.Validation.ValidationFailed, response.Message);
            Assert.NotNull(response.Errors);

            if (pageNumber < 1) Assert.True(response.Errors.ContainsKey(nameof(query.PageNumber)));
            if (pageSize < 1) Assert.True(response.Errors.ContainsKey(nameof(query.PageSize)));
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenPageSizeExceedsMax()
        {
            // Arrange
            var query = new PaginationQueryParameters { PageNumber = 1, PageSize = 100 }; // 100 > 10
            var actionArguments = new Dictionary<string, object?> { { "query", query } };
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            var expectedErrorMessage = string.Format(Global.Validation.PageSizeExceeded, _filter.MaxPageSize);
            Assert.Equal(expectedErrorMessage, response.Errors![nameof(query.PageSize)][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenBindingFails()
        {
            // Arrange
            var context = CreateContext(new Dictionary<string, object?>());
            const string propertyName = "PageNumber";
            const string attemptedValue = "not-a-number";

            context.ModelState.SetModelValue(propertyName, attemptedValue, attemptedValue);
            context.ModelState.AddModelError(propertyName, "The value is invalid.");

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            var expectedError = string.Format(Global.Validation.InvalidNumberFormat, attemptedValue);
            Assert.Equal(expectedError, response.Errors![propertyName][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldDoNothing_WhenParametersAreValid()
        {
            // Arrange
            var query = new PaginationQueryParameters { PageNumber = 1, PageSize = 10 };
            var actionArguments = new Dictionary<string, object?> { { "query", query } };
            var context = CreateContext(actionArguments);

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }
    }
}
