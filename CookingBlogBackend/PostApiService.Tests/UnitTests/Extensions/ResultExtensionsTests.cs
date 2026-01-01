using PostApiService.Infrastructure.Common;
using PostApiService.Extensions;

namespace PostApiService.Tests.UnitTests.Extensions
{
    public class ResultExtensionsTests
    {
        [Theory]
        [InlineData(ResultStatus.Success, "Data", "Msg", typeof(OkObjectResult), 200)]
        [InlineData(ResultStatus.NoContent, null, null, typeof(NoContentResult), 204)]
        [InlineData(ResultStatus.NoContent, null, "Deleted", typeof(OkObjectResult), 200)]
        [InlineData(ResultStatus.NotFound, null, "Err", typeof(NotFoundObjectResult), 404)]
        [InlineData(ResultStatus.Conflict, null, "Err", typeof(ConflictObjectResult), 409)]
        [InlineData(ResultStatus.Invalid, null, "Err", typeof(BadRequestObjectResult), 400)]
        public void ToActionResult_ShouldReturnCorrectResult_ForAllStatuses(
            ResultStatus status, object? value, string? message, Type expectedType, int expectedStatusCode)
        {
            // Arrange            
            var resultType = typeof(Result<>).MakeGenericType(value?.GetType() ?? typeof(object));
            var result = Activator.CreateInstance(resultType, System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance, null, new[] { value, status, "Error Message" }, null);

            var testResult = Result<object>.Success(value!);
            var genericResult = CreateResultForTest(status, value, "Err");

            // Act
            var actionResult = genericResult.ToActionResult(message);

            // Assert
            Assert.IsType(expectedType, actionResult);
            if (actionResult is ObjectResult objectResult)
            {
                Assert.Equal(expectedStatusCode, objectResult.StatusCode);
                var apiResponse = objectResult.Value as ApiResponse;
                Assert.NotNull(apiResponse);
                if (message != null) Assert.Equal(message, apiResponse.Message);
            }
            else if (actionResult is StatusCodeResult statusCodeResult)
            {
                Assert.Equal(expectedStatusCode, statusCodeResult.StatusCode);
            }
        }

        [Theory]
        [InlineData(ResultStatus.Success, typeof(CreatedAtActionResult), 201)]
        [InlineData(ResultStatus.NotFound, typeof(NotFoundObjectResult), 404)]
        public void ToCreatedResult_ShouldHandleSuccessAndDelegation(
            ResultStatus status, Type expectedType, int expectedStatusCode)
        {
            // Arrange
            var result = status == ResultStatus.Success
                ? Result<string>.Success("data")
                : Result<string>.NotFound("error");

            var routeValues = new { id = 1 };

            // Act
            var actionResult = result.ToCreatedResult("Action", routeValues);

            // Assert
            Assert.IsType(expectedType, actionResult);
            var objectResult = actionResult as ObjectResult;
            Assert.Equal(expectedStatusCode, objectResult?.StatusCode);
        }

        private Result<object> CreateResultForTest(ResultStatus status, object? val, string err)
        {
            var ctor = typeof(Result<object>).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new[] { typeof(object), typeof(ResultStatus), typeof(string) }, null);
            return (Result<object>)ctor!.Invoke(new[] { val!, status, err });
        }
    }
}
