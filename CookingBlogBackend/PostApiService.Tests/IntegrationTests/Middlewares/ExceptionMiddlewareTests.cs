using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Dto;
using System.Net;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Security.Claims;

namespace PostApiService.Tests.IntegrationTests.Middlewares
{
    public class ExceptionMiddlewareTests : IClassFixture<ExceptionMiddlewareFixture>
    {
        public const string AuthApiEndpoint = "/api/auth/register";
        public const string LoginApiEndpoint = "/api/auth/login";
        public const string PostsApiEndpoint = "/api/posts";
        public const string CommentsApiEndpoint = "/api/comments";

        private readonly HttpClient _client;
        private readonly ExceptionMiddlewareFixture _factoryFixture;

        public ExceptionMiddlewareTests(ExceptionMiddlewareFixture factoryFixture)
        {
            _factoryFixture = factoryFixture;
            _client = _factoryFixture.Client;
        }

        [Theory]
        [InlineData(typeof(UserAlreadyExistsException), AuthApiEndpoint, HttpStatusCode.Conflict)]
        [InlineData(typeof(EmailAlreadyExistsException), AuthApiEndpoint, HttpStatusCode.Conflict)]
        [InlineData(typeof(UserClaimException), AuthApiEndpoint, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(UserCreationException), AuthApiEndpoint, HttpStatusCode.InternalServerError)]
        public async Task RegisterUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange                        
            var authServiceMock = _factoryFixture?.Services?.GetRequiredService<IAuthService>();

            authServiceMock.ClearReceivedCalls();

            switch (exceptionType)
            {
                case Type t when t == typeof(UserAlreadyExistsException):
                    authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new UserAlreadyExistsException(RegisterErrorMessages.UsernameAlreadyExists)));
                    break;
                case Type t when t == typeof(EmailAlreadyExistsException):
                    authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new EmailAlreadyExistsException(RegisterErrorMessages.EmailAlreadyExists)));
                    break;
                case Type t when t == typeof(UserClaimException):
                    authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new UserClaimException(RegisterErrorMessages.CreationFailed)));
                    break;
                case Type t when t == typeof(UserCreationException):
                    authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new UserCreationException(RegisterErrorMessages.CreationFailed)));
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
                Type t when t == typeof(UserAlreadyExistsException) => RegisterErrorMessages.UsernameAlreadyExists,
                Type t when t == typeof(EmailAlreadyExistsException) => RegisterErrorMessages.EmailAlreadyExists,
                Type t when t == typeof(UserClaimException) => RegisterErrorMessages.CreationFailed,
                Type t when t == typeof(UserCreationException) => RegisterErrorMessages.CreationFailed
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(AuthenticationException), LoginApiEndpoint, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(UnauthorizedAccessException), LoginApiEndpoint, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(UserNotFoundException), LoginApiEndpoint, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(ArgumentException), LoginApiEndpoint, HttpStatusCode.InternalServerError)]
        public async Task LoginUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            var authServiceMock = _factoryFixture?.Services?.GetRequiredService<IAuthService>();

            authServiceMock.ClearReceivedCalls();

            Task<IdentityUser> failedTask = exceptionType switch
            {
                Type t when t == typeof(AuthenticationException) =>
                    Task.FromException<IdentityUser>(new AuthenticationException(AuthErrorMessages.InvalidCredentials)),
                Type t when t == typeof(UnauthorizedAccessException) =>
                    Task.FromException<IdentityUser>(new UnauthorizedAccessException(AuthErrorMessages.UnauthorizedAccess)),
                Type t when t == typeof(UserNotFoundException) =>
                    Task.FromException<IdentityUser>(new UserNotFoundException(AuthErrorMessages.InvalidCredentials)),
                Type t when t == typeof(ArgumentException) =>
                    Task.FromException<IdentityUser>(new ArgumentException(TokenErrorMessages.GenerationFailed)),
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
                Type t when t == typeof(AuthenticationException) => AuthErrorMessages.InvalidCredentials,
                Type t when t == typeof(UnauthorizedAccessException) => AuthErrorMessages.UnauthorizedAccess,
                Type t when t == typeof(UserNotFoundException) => AuthErrorMessages.InvalidCredentials,
                Type t when t == typeof(ArgumentException) => TokenErrorMessages.GenerationFailed,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }
       
        [Theory]
        [InlineData(typeof(OperationCanceledException), PostsApiEndpoint, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), PostsApiEndpoint, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), PostsApiEndpoint, HttpStatusCode.InternalServerError)]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var postServiceMock = _factoryFixture?.Services?.GetRequiredService<IPostService>();

            postServiceMock.ClearReceivedCalls();

            Exception exceptionToThrow = exceptionType switch
            {
                Type t when t == typeof(OperationCanceledException) =>
                    new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) =>
                    new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) =>
                    new Exception(ResponseErrorMessages.UnexpectedErrorException),
            };

            var failedTupleTask = Task.FromException<(List<PostListDto> Posts, int TotalCount)>(exceptionToThrow);

            postServiceMock?.GetPostsWithTotalPostCountAsync
               (Arg.Any<int>(),
               Arg.Any<int>(),
               Arg.Any<CancellationToken>())
               .Returns(failedTupleTask);

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

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{PostsApiEndpoint}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(OperationCanceledException), $"{PostsApiEndpoint}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{PostsApiEndpoint}/2", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{PostsApiEndpoint}/3", HttpStatusCode.InternalServerError)]
        public async Task GetPostByIdAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var invalidPostId = 999;
            var postServiceMock = _factoryFixture?.Services?.GetRequiredService<IPostService>();

            postServiceMock.ClearReceivedCalls();

            Task<Post> failedTask = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) =>
                    Task.FromException<Post>(new PostNotFoundException(invalidPostId)),
                Type t when t == typeof(OperationCanceledException) =>
                    Task.FromException<Post>(new OperationCanceledException(ResponseErrorMessages.OperationCanceledException)),
                Type t when t == typeof(TimeoutException) =>
                    Task.FromException<Post>(new TimeoutException(ResponseErrorMessages.TimeoutException)),
                Type t when t == typeof(Exception) =>
                    Task.FromException<Post>(new Exception(ResponseErrorMessages.UnexpectedErrorException)),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            postServiceMock?.GetPostByIdAsync(Arg.Any<int>(), Arg.Any<bool>())
                .Returns(failedTask);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => string.Format(PostErrorMessages.PostNotFound, invalidPostId),
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostAlreadyExistException), PostsApiEndpoint, HttpStatusCode.Conflict)]
        [InlineData(typeof(AddPostFailedException), PostsApiEndpoint, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), PostsApiEndpoint, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), PostsApiEndpoint, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), PostsApiEndpoint, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), PostsApiEndpoint, HttpStatusCode.InternalServerError)]
        public async Task AddPostAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _factoryFixture.SetCurrentUser(adminPrincipal);

            var postTitle = "Test title";
            var postServiceMock = _factoryFixture?.Services?.GetRequiredService<IPostService>();

            postServiceMock.ClearReceivedCalls();

            Task<Post> failedTask = exceptionType switch
            {
                Type t when t == typeof(PostAlreadyExistException) =>
                    Task.FromException<Post>(new PostAlreadyExistException(postTitle)),
                Type t when t == typeof(AddPostFailedException) =>
                    Task.FromException<Post>(new AddPostFailedException(postTitle, null)),
                Type t when t == typeof(DbUpdateException) =>
                    Task.FromException<Post>(new DbUpdateException(ResponseErrorMessages.DbUpdateException)),
                Type t when t == typeof(OperationCanceledException) =>
                    Task.FromException<Post>(new OperationCanceledException(ResponseErrorMessages.OperationCanceledException)),
                Type t when t == typeof(TimeoutException) =>
                    Task.FromException<Post>(new TimeoutException(ResponseErrorMessages.TimeoutException)),
                Type t when t == typeof(Exception) =>
                    Task.FromException<Post>(new Exception(ResponseErrorMessages.UnexpectedErrorException)),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            postServiceMock?.AddPostAsync(Arg.Any<Post>())
                .Returns(failedTask);

            var newPost = TestDataHelper.GetSinglePost();
            var content = HttpHelper.GetJsonHttpContent(newPost);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(PostAlreadyExistException) => string.Format
                (PostErrorMessages.PostAlreadyExist, postTitle),
                Type t when t == typeof(AddPostFailedException) => string.Format
                (PostErrorMessages.AddPostFailed, postTitle),
                Type t when t == typeof(DbUpdateException) => ResponseErrorMessages.DbUpdateException,
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{PostsApiEndpoint}/1", HttpStatusCode.NotFound)]
        [InlineData(typeof(UpdatePostFailedException), $"{PostsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{PostsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{PostsApiEndpoint}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{PostsApiEndpoint}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{PostsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        public async Task UpdatePostAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _factoryFixture.SetCurrentUser(adminPrincipal);

            var testPostId = 999;
            var postTitle = "Test title";

            var postServiceMock = _factoryFixture?.Services?.GetRequiredService<IPostService>();

            postServiceMock.ClearReceivedCalls();

            switch (exceptionType)
            {
                case Type t when t == typeof(PostNotFoundException):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new PostNotFoundException(testPostId)));
                    break;
                case Type t when t == typeof(UpdatePostFailedException):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new UpdatePostFailedException(postTitle, null)));
                    break;
                case Type t when t == typeof(DbUpdateException):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new DbUpdateException(ResponseErrorMessages.DbUpdateException)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new OperationCanceledException(ResponseErrorMessages.OperationCanceledException)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new TimeoutException(ResponseErrorMessages.TimeoutException)));
                    break;
                case Type t when t == typeof(Exception):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new Exception(ResponseErrorMessages.UnexpectedErrorException)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            var post = TestDataHelper.GetSinglePost();
            post.Title = "Updated post title";

            var content = HttpHelper.GetJsonHttpContent(post);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => string.Format
                (PostErrorMessages.PostNotFound, testPostId),
                Type t when t == typeof(UpdatePostFailedException) => string.Format
                (PostErrorMessages.UpdatePostFailed, postTitle),
                Type t when t == typeof(DbUpdateException) => ResponseErrorMessages.DbUpdateException,
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{PostsApiEndpoint}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(DeletePostFailedException), $"{PostsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{PostsApiEndpoint}/2", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{PostsApiEndpoint}/3", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{PostsApiEndpoint}/4", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{PostsApiEndpoint}/5", HttpStatusCode.InternalServerError)]
        public async Task DeletePostAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _factoryFixture.SetCurrentUser(adminPrincipal);

            var testPostId = 999;
            var postServiceMock = _factoryFixture?.Services?.GetRequiredService<IPostService>();

            postServiceMock.ClearReceivedCalls();

            switch (exceptionType)
            {
                case Type t when t == typeof(PostNotFoundException):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new PostNotFoundException(testPostId)));
                    break;
                case Type t when t == typeof(DeletePostFailedException):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new DeletePostFailedException(testPostId, null)));
                    break;
                case Type t when t == typeof(DbUpdateException):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new DbUpdateException(ResponseErrorMessages.DbUpdateException)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new OperationCanceledException(ResponseErrorMessages.OperationCanceledException)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new TimeoutException(ResponseErrorMessages.TimeoutException)));
                    break;
                case Type t when t == typeof(Exception):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new Exception(ResponseErrorMessages.UnexpectedErrorException)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => string.Format
                (PostErrorMessages.PostNotFound, testPostId),
                Type t when t == typeof(DeletePostFailedException) => string.Format
                (PostErrorMessages.DeletePostFailed, testPostId),
                Type t when t == typeof(DbUpdateException) => ResponseErrorMessages.DbUpdateException,
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{CommentsApiEndpoint}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(AddCommentFailedException), $"{CommentsApiEndpoint}/999", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{CommentsApiEndpoint}/999", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{CommentsApiEndpoint}/999", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{CommentsApiEndpoint}/999", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{CommentsApiEndpoint}/999", HttpStatusCode.InternalServerError)]
        public async Task AddCommentAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "cont-id"),
                new Claim(ClaimTypes.Name, "cont"),
                new Claim(ClaimTypes.Role, "Contributor"),
                new Claim("Comment", "2"),
                new Claim("Comment", "3"),
                new Claim("Comment", "4")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _factoryFixture.SetCurrentUser(adminPrincipal);

            var testPostId = 999;
            var commentServiceMock = _factoryFixture?.Services?.GetRequiredService<ICommentService>();

            commentServiceMock.ClearReceivedCalls();

            switch (exceptionType)
            {
                case Type t when t == typeof(PostNotFoundException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new PostNotFoundException(testPostId)));
                    break;
                case Type t when t == typeof(AddCommentFailedException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new AddCommentFailedException(testPostId, null)));
                    break;
                case Type t when t == typeof(DbUpdateException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new DbUpdateException(ResponseErrorMessages.DbUpdateException)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new OperationCanceledException(ResponseErrorMessages.OperationCanceledException)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new TimeoutException(ResponseErrorMessages.TimeoutException)));
                    break;
                case Type t when t == typeof(Exception):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new Exception(ResponseErrorMessages.UnexpectedErrorException)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            var comment = new Comment { PostId = testPostId, Content = "Test comment" };
            var content = HttpHelper.GetJsonHttpContent(comment);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Comment>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => string.Format
                (PostErrorMessages.PostNotFound, testPostId),
                Type t when t == typeof(AddCommentFailedException) => string.Format
                (CommentErrorMessages.AddCommentFailed, testPostId),
                Type t when t == typeof(DbUpdateException) => ResponseErrorMessages.DbUpdateException,
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(CommentNotFoundException), $"{CommentsApiEndpoint}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(UpdateCommentFailedException), $"{CommentsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{CommentsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{CommentsApiEndpoint}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{CommentsApiEndpoint}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{CommentsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        public async Task UpdateCommentAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange            
            var testCommentId = 999;

            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "cont-id"),
                new Claim(ClaimTypes.Name, "cont"),
                new Claim(ClaimTypes.Role, "Contributor"),
                new Claim("Comment", "2"),
                new Claim("Comment", "3"),
                new Claim("Comment", "4"),
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _factoryFixture.SetCurrentUser(adminPrincipal);

            var commentServiceMock = _factoryFixture?.Services?.GetRequiredService<ICommentService>();

            commentServiceMock.ClearReceivedCalls();

            switch (exceptionType)
            {
                case Type t when t == typeof(CommentNotFoundException):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new CommentNotFoundException(testCommentId)));
                    break;
                case Type t when t == typeof(UpdateCommentFailedException):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new UpdateCommentFailedException(testCommentId, null)));
                    break;
                case Type t when t == typeof(DbUpdateException):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new DbUpdateException(ResponseErrorMessages.DbUpdateException)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new OperationCanceledException(ResponseErrorMessages.OperationCanceledException)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new TimeoutException(ResponseErrorMessages.TimeoutException)));
                    break;
                case Type t when t == typeof(Exception):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new Exception(ResponseErrorMessages.UnexpectedErrorException)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            var updatedComment = new EditCommentModel { Content = "Test comment" };
            var content = HttpHelper.GetJsonHttpContent(updatedComment);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Comment>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(CommentNotFoundException) => string.Format
                (CommentErrorMessages.CommentNotFound, testCommentId),
                Type t when t == typeof(UpdateCommentFailedException) => string.Format
                (CommentErrorMessages.UpdateCommentFailed, testCommentId),
                Type t when t == typeof(DbUpdateException) => ResponseErrorMessages.DbUpdateException,
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(CommentNotFoundException), $"{CommentsApiEndpoint}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(DeleteCommentFailedException), $"{CommentsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{CommentsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{CommentsApiEndpoint}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{CommentsApiEndpoint}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{CommentsApiEndpoint}/1", HttpStatusCode.InternalServerError)]
        public async Task DeleteCommentAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange            
            var testCommentId = 999;

            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "cont-id"),
                new Claim(ClaimTypes.Name, "cont"),
                new Claim(ClaimTypes.Role, "Contributor"),
                new Claim("Comment", "2"),
                new Claim("Comment", "3"),
                new Claim("Comment", "4"),
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _factoryFixture.SetCurrentUser(adminPrincipal);

            var commentServiceMock = _factoryFixture?.Services?.GetRequiredService<ICommentService>();

            commentServiceMock.ClearReceivedCalls();

            switch (exceptionType)
            {
                case Type t when t == typeof(CommentNotFoundException):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new CommentNotFoundException(testCommentId)));
                    break;
                case Type t when t == typeof(DeleteCommentFailedException):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new DeleteCommentFailedException(testCommentId, null)));
                    break;
                case Type t when t == typeof(DbUpdateException):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new DbUpdateException(ResponseErrorMessages.DbUpdateException)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                         .Returns(Task.FromException(new OperationCanceledException(ResponseErrorMessages.OperationCanceledException)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new TimeoutException(ResponseErrorMessages.TimeoutException)));
                    break;
                case Type t when t == typeof(Exception):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                         .Returns(Task.FromException(new Exception(ResponseErrorMessages.UnexpectedErrorException)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            var updatedComment = new EditCommentModel { Content = "Test comment" };
            var content = HttpHelper.GetJsonHttpContent(updatedComment);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Comment>>();
            Assert.NotNull(errorResponse);

            string expectedMessage = exceptionType switch
            {
                Type t when t == typeof(CommentNotFoundException) => string.Format
                (CommentErrorMessages.CommentNotFound, testCommentId),
                Type t when t == typeof(DeleteCommentFailedException) => string.Format
                (CommentErrorMessages.DeleteCommentFailed, testCommentId),
                Type t when t == typeof(DbUpdateException) => ResponseErrorMessages.DbUpdateException,
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }
    }
}