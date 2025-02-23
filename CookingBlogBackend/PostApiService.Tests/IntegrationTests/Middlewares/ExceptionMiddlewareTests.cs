using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Middlewares
{
    public class ExceptionMiddlewareTests : IClassFixture<ExceptionMiddlewareFixture>
    {
        private readonly HttpClient _client;
        private readonly ExceptionMiddlewareFixture _factoryFixture;

        public ExceptionMiddlewareTests(ExceptionMiddlewareFixture factoryFixture)
        {
            _factoryFixture = factoryFixture;
            _client = _factoryFixture.Client;
        }

        [Theory]
        [InlineData(typeof(OperationCanceledException), "/api/posts", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/posts", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/posts", HttpStatusCode.InternalServerError)]
        public async Task GetAllPostsAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            Exception exception = exceptionType switch
            {
                Type t when t == typeof(OperationCanceledException) =>
                new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) => new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }
    }
}
