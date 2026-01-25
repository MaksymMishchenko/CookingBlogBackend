using Microsoft.Extensions.DependencyInjection;
using NSubstitute.ExceptionExtensions;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Middlewares
{
    public class ExceptionMiddlewareTests : IClassFixture<ExceptionMiddlewareFixture>
    {
        private readonly ExceptionMiddlewareFixture _fixture;
        private readonly HttpClient _client;

        public ExceptionMiddlewareTests(ExceptionMiddlewareFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
            _fixture.ClearMocks();
        }

        [Theory]
        [InlineData(typeof(DbUpdateException), HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TaskCanceledException), HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), HttpStatusCode.InternalServerError)]
        public async Task RegisterUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, HttpStatusCode expectedStatus)
        {
            // Arrange
            string url = Authentication.Register;
            var authServiceMock = _fixture?.Services?.GetRequiredService<IAuthService>();

            var exception = exceptionType switch
            {
                Type t when t == typeof(DbUpdateException) =>
                    new DbUpdateException(Global.System.DbUpdateError),
                Type t when t == typeof(OperationCanceledException) =>
                    new OperationCanceledException(Global.System.RequestCancelled),
                Type t when t == typeof(TaskCanceledException) =>
                    new TaskCanceledException(Global.System.RequestCancelled),
                Type t when t == typeof(TimeoutException) =>
                    new TimeoutException(Global.System.Timeout),
                Type t when t == typeof(Exception) =>
                   new Exception(Global.System.DatabaseCriticalError),
                _ => new Exception($"Unsupported exception type: {exceptionType}")
            };

            authServiceMock!.RegisterUserAsync(Arg.Any<RegisterUserDto>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var newUser = AuthTestData.CreateRegisterUserDto();

            // Act
            var response = await _client.PostAsJsonAsync(url, newUser);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {                
                Type t when t == typeof(OperationCanceledException) ||
                            t == typeof(TaskCanceledException) ||
                            t == typeof(TimeoutException)
                    => Global.System.Timeout,
                
                Type t when t == typeof(DbUpdateException)
                    => Global.System.DbUpdateError,               
                _ => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);

            await authServiceMock!.Received(1).RegisterUserAsync(
                Arg.Any<RegisterUserDto>(), Arg.Any<CancellationToken>());
        }
       
        [Fact]
        public async Task AuthenticateAsync_ShouldReturn500_WhenTokenGenerationFails()
        {
            // Arrange
            var authServiceMock = _fixture?.Services?.GetRequiredService<IAuthService>();
            var loginDto = AuthTestData.CreateUserLoginDto();

            authServiceMock!.AuthenticateAsync(Arg.Any<LoginUserDto>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new ArgumentException(Auth.Token.Errors.GenerationFailed));

            // Act
            var response = await _client.PostAsJsonAsync(Authentication.Login, loginDto);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.Equal(Global.System.DatabaseCriticalError, errorResponse!.Message);

            await authServiceMock!.Received(1).AuthenticateAsync(Arg.Any<LoginUserDto>(),
                Arg.Any<CancellationToken>());
        }
    }
}