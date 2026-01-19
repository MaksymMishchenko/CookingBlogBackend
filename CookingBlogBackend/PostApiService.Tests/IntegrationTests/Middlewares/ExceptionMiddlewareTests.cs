using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;
using System.Security.Authentication;

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
        [InlineData(typeof(UserAlreadyExistsException), Authentication.Register, HttpStatusCode.Conflict)]
        [InlineData(typeof(EmailAlreadyExistsException), Authentication.Register, HttpStatusCode.Conflict)]
        [InlineData(typeof(UserClaimException), Authentication.Register, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(UserCreationException), Authentication.Register, HttpStatusCode.InternalServerError)]
        public async Task RegisterUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange                        
            var authServiceMock = _fixture?.Services?.GetRequiredService<IAuthService>();

            switch (exceptionType)
            {
                case Type t when t == typeof(UserAlreadyExistsException):
                    authServiceMock!.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new UserAlreadyExistsException(Auth.Registration.Errors.UsernameAlreadyExists)));
                    break;
                case Type t when t == typeof(EmailAlreadyExistsException):
                    authServiceMock!.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new EmailAlreadyExistsException(Auth.Registration.Errors.EmailAlreadyExists)));
                    break;
                case Type t when t == typeof(UserClaimException):
                    authServiceMock!.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new UserClaimException(Auth.Registration.Errors.CreationFailed)));
                    break;
                case Type t when t == typeof(UserCreationException):
                    authServiceMock!.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new UserCreationException(Auth.Registration.Errors.CreationFailed)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            var newUser = new RegisterUser { UserName = "testUser", Email = "test@test.com", Password = "Rtyuehe3-" };
            var content = HttpHelper.GetJsonHttpContent(newUser);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterUser>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(UserAlreadyExistsException) => Auth.Registration.Errors.UsernameAlreadyExists,
                Type t when t == typeof(EmailAlreadyExistsException) => Auth.Registration.Errors.EmailAlreadyExists,
                Type t when t == typeof(UserClaimException) => Auth.Registration.Errors.CreationFailed,
                Type t when t == typeof(UserCreationException) => Auth.Registration.Errors.CreationFailed,
                _ => throw new ArgumentOutOfRangeException(nameof(exceptionType), "Unexpected exception type")
            };

            Assert.Equal(expectedMessage, errorResponse.Message);

            await authServiceMock.Received(1).RegisterUserAsync(Arg.Is<RegisterUser>(u => u.UserName == "testUser"));
        }

        [Theory]
        [InlineData(typeof(AuthenticationException), Authentication.Login, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(UnauthorizedAccessException), Authentication.Login, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(UserNotFoundException), Authentication.Login, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(ArgumentException), Authentication.Login, HttpStatusCode.InternalServerError)]
        public async Task LoginUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            var authServiceMock = _fixture?.Services?.GetRequiredService<IAuthService>();

            Task<IdentityUser> failedTask = exceptionType switch
            {
                Type t when t == typeof(AuthenticationException) =>
                    Task.FromException<IdentityUser>(new AuthenticationException(Auth.LoginM.Errors.InvalidCredentials)),
                Type t when t == typeof(UnauthorizedAccessException) =>
                    Task.FromException<IdentityUser>(new UnauthorizedAccessException(Auth.LoginM.Errors.UnauthorizedAccess)),
                Type t when t == typeof(UserNotFoundException) =>
                    Task.FromException<IdentityUser>(new UserNotFoundException(Auth.LoginM.Errors.InvalidCredentials)),
                Type t when t == typeof(ArgumentException) =>
                    Task.FromException<IdentityUser>(new ArgumentException(Auth.Token.Errors.GenerationFailed)),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            authServiceMock?.LoginAsync(Arg.Any<LoginUser>()).Returns(failedTask);

            var user = new LoginUser { UserName = "testUser", Password = "Rtyuehe3-" };
            var content = HttpHelper.GetJsonHttpContent(user);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginUser>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(AuthenticationException) => Auth.LoginM.Errors.InvalidCredentials,
                Type t when t == typeof(UnauthorizedAccessException) => Auth.LoginM.Errors.UnauthorizedAccess,
                Type t when t == typeof(UserNotFoundException) => Auth.LoginM.Errors.InvalidCredentials,
                Type t when t == typeof(ArgumentException) => Auth.Token.Errors.GenerationFailed,
                Type t when t == typeof(Exception) => Global.Validation.UnexpectedErrorException,
                _ => throw new ArgumentOutOfRangeException(nameof(exceptionType), "Unexpected exception type")
            };

            Assert.Equal(expectedMessage, errorResponse.Message);

            await authServiceMock.Received(1)!.LoginAsync(Arg.Is<LoginUser>(u => u.UserName == "testUser"));
        }

        [Theory]
        [InlineData(typeof(DbUpdateException), Posts.Base, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), Posts.Base, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), Posts.Base, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), Posts.Base, HttpStatusCode.InternalServerError)]
        public async Task AddPostAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange                             
            _fixture.LoginAsAdmin();
            var postServiceMock = _fixture?.Services?.GetRequiredService<IPostService>();

            var exception = exceptionType switch
            {
                Type t when t == typeof(DbUpdateException) =>
                    new DbUpdateException(Global.System.DbUpdateError),
                Type t when t == typeof(OperationCanceledException) =>
                    new OperationCanceledException(Global.System.RequestCancelled),
                Type t when t == typeof(TimeoutException) =>
                    new TimeoutException(Global.System.Timeout),
                Type t when t == typeof(Exception) =>
                   new Exception(Global.System.DatabaseCriticalError),
                _ => new Exception($"Unsupported exception type: {exceptionType}")
            };

            postServiceMock?.AddPostAsync(Arg.Any<PostCreateDto>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<Result<PostAdminDetailsDto>>(exception));

            var postDto = TestDataHelper.GetPostCreateDto();
            var content = HttpHelper.GetJsonHttpContent(postDto);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError,
                _ => throw new ArgumentOutOfRangeException(nameof(exceptionType), "Unexpected exception type")
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
            await postServiceMock.Received(1)!.AddPostAsync(Arg.Any<PostCreateDto>(), Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(typeof(DbUpdateException), $"{Comments.Base}/999", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{Comments.Base}/999", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{Comments.Base}/999", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{Comments.Base}/999", HttpStatusCode.InternalServerError)]
        public async Task AddCommentAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange                       
            _fixture.LoginAsContributor();
            var commentServiceMock = _fixture?.Services?.GetRequiredService<ICommentService>();

            switch (exceptionType)
            {
                case Type t when t == typeof(DbUpdateException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromException<Result<CommentCreatedDto>>(new DbUpdateException(Global.System.DbUpdateError)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromException<Result<CommentCreatedDto>>(new OperationCanceledException(Global.System.RequestCancelled)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromException<Result<CommentCreatedDto>>(new TimeoutException(Global.System.Timeout)));
                    break;
                case Type t when t == typeof(Exception):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromException<Result<CommentCreatedDto>>(new Exception(Global.System.DatabaseCriticalError)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            var createDto = new CommentCreateDto { Content = "Test comment" };
            var content = HttpHelper.GetJsonHttpContent(createDto);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError,
                _ => throw new ArgumentOutOfRangeException(nameof(exceptionType), "Unexpected exception type")
            };

            Assert.Equal(expectedMessage, errorResponse.Message);

            await commentServiceMock.Received(1)!.AddCommentAsync(
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}