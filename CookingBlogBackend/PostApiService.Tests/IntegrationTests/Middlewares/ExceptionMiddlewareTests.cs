using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;
using System.Net.Http.Json;
using System.Security.Authentication;

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
        [InlineData(typeof(UserAlreadyExistsException), "/api/auth/register", HttpStatusCode.Conflict)]
        [InlineData(typeof(EmailAlreadyExistsException), "/api/auth/register", HttpStatusCode.Conflict)]
        [InlineData(typeof(UserClaimException), "/api/auth/register", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(UserCreationException), "/api/auth/register", HttpStatusCode.InternalServerError)]
        public async Task RegisterUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            Exception exception = exceptionType switch
            {
                Type t when t == typeof(UserAlreadyExistsException) =>
                new UserAlreadyExistsException(RegisterErrorMessages.UsernameAlreadyExists),
                Type t when t == typeof(EmailAlreadyExistsException) =>
                new EmailAlreadyExistsException(RegisterErrorMessages.EmailAlreadyExists),
                Type t when t == typeof(UserClaimException) =>
                new UserClaimException(RegisterErrorMessages.CreationFailed),
                Type t when t == typeof(UserCreationException) =>
                new UserCreationException(RegisterErrorMessages.CreationFailed),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

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
                Type t when t == typeof(UserCreationException) => RegisterErrorMessages.CreationFailed,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(AuthenticationException), "/api/auth/login", HttpStatusCode.Unauthorized)]
        [InlineData(typeof(UserNotFoundException), "/api/auth/login", HttpStatusCode.Unauthorized)]
        [InlineData(typeof(ArgumentException), "/api/auth/login", HttpStatusCode.InternalServerError)]
        public async Task LoginUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            Exception exception = exceptionType switch
            {
                Type t when t == typeof(AuthenticationException) =>
                new AuthenticationException(AuthErrorMessages.InvalidCredentials),

                Type t when t == typeof(UserNotFoundException) =>
                new UserNotFoundException(AuthErrorMessages.InvalidCredentials),

                Type t when t == typeof(ArgumentException) =>
                new ArgumentException(TokenErrorMessages.GenerationFailed),

                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

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
                Type t when t == typeof(UserNotFoundException) => AuthErrorMessages.InvalidCredentials,
                Type t when t == typeof(ArgumentException) => TokenErrorMessages.GenerationFailed,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
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

        [Theory]
        [InlineData(typeof(PostNotFoundException), "/api/Posts/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(OperationCanceledException), "/api/Posts/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/Posts/2", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/Posts/4", HttpStatusCode.InternalServerError)]
        public async Task GetPostByIdAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var invalidPostId = 999;

            Exception exception = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => new PostNotFoundException(invalidPostId),
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
                Type t when t == typeof(PostNotFoundException) => string.Format(PostErrorMessages.PostNotFound, invalidPostId),
                Type t when t == typeof(OperationCanceledException) => ResponseErrorMessages.OperationCanceledException,
                Type t when t == typeof(TimeoutException) => ResponseErrorMessages.TimeoutException,
                Type t when t == typeof(Exception) => ResponseErrorMessages.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostAlreadyExistException), "/api/posts", HttpStatusCode.Conflict)]
        [InlineData(typeof(AddPostFailedException), "/api/posts", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), "/api/posts", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), "/api/posts", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/posts", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/posts", HttpStatusCode.InternalServerError)]
        public async Task AddPostAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var postTitle = "Test title";

            Exception exception = exceptionType switch
            {
                Type t when t == typeof(PostAlreadyExistException) => new PostAlreadyExistException(postTitle),
                Type t when t == typeof(AddPostFailedException) => new AddPostFailedException(postTitle),
                Type t when t == typeof(DbUpdateException) => new DbUpdateException(ResponseErrorMessages.DbUpdateException),
                Type t when t == typeof(OperationCanceledException) =>
                new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) => new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

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
        [InlineData(typeof(PostNotFoundException), "/api/Posts", HttpStatusCode.NotFound)]
        [InlineData(typeof(UpdatePostFailedException), "/api/Posts", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), "/api/Posts", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), "/api/Posts", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/Posts", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/Posts", HttpStatusCode.InternalServerError)]
        public async Task UpdatePostAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var testPostId = 999;
            var postTitle = "Test title";

            Exception exception = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => new PostNotFoundException(testPostId),
                Type t when t == typeof(UpdatePostFailedException) => new UpdatePostFailedException(postTitle),
                Type t when t == typeof(DbUpdateException) => new DbUpdateException(ResponseErrorMessages.DbUpdateException),
                Type t when t == typeof(OperationCanceledException) =>
                new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) => new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

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
        [InlineData(typeof(PostNotFoundException), "/api/Posts/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(DeletePostFailedException), "/api/Posts/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), "/api/Posts/2", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), "/api/Posts/3", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/Posts/4", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/Posts/5", HttpStatusCode.InternalServerError)]
        public async Task DeletePostAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var testPostId = 999;

            Exception exception = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => new PostNotFoundException(testPostId),
                Type t when t == typeof(DeletePostFailedException) => new DeletePostFailedException(testPostId),
                Type t when t == typeof(DbUpdateException) => new DbUpdateException(ResponseErrorMessages.DbUpdateException),
                Type t when t == typeof(OperationCanceledException) =>
                new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) => new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

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
        [InlineData(typeof(PostNotFoundException), "/api/comments/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(AddCommentFailedException), "/api/comments/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), "/api/comments/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), "/api/comments/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/comments/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/comments/1", HttpStatusCode.InternalServerError)]
        public async Task AddCommentAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange            
            var testPostId = 999;

            Exception exception = exceptionType switch
            {
                Type t when t == typeof(PostNotFoundException) => new PostNotFoundException(testPostId),
                Type t when t == typeof(AddCommentFailedException) => new AddCommentFailedException(testPostId),
                Type t when t == typeof(DbUpdateException) => new DbUpdateException(ResponseErrorMessages.DbUpdateException),
                Type t when t == typeof(OperationCanceledException) =>
                new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) => new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

            var comment = new Comment { Content = "Test comment" };

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
        [InlineData(typeof(CommentNotFoundException), "/api/comments/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(UpdateCommentFailedException), "/api/comments/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), "/api/comments/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), "/api/comments/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/comments/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/comments/1", HttpStatusCode.InternalServerError)]
        public async Task UpdateCommentAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange            
            var testCommentId = 999;

            Exception exception = exceptionType switch
            {
                Type t when t == typeof(CommentNotFoundException) => new CommentNotFoundException(testCommentId),
                Type t when t == typeof(UpdateCommentFailedException) => new UpdateCommentFailedException(testCommentId),
                Type t when t == typeof(DbUpdateException) => new DbUpdateException(ResponseErrorMessages.DbUpdateException),
                Type t when t == typeof(OperationCanceledException) =>
                new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) => new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

            var updatedComment = new EditCommentModel { Content = "Test comment" };
            var content = HttpHelper.GetJsonHttpContent(updatedComment);

            // Act
            var response = await _client.PostAsync(url, content);

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
        [InlineData(typeof(CommentNotFoundException), "/api/comments/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(DeleteCommentFailedException), "/api/comments/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), "/api/comments/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), "/api/comments/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), "/api/comments/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), "/api/comments/1", HttpStatusCode.InternalServerError)]
        public async Task DeleteCommentAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange            
            var testCommentId = 999;

            Exception exception = exceptionType switch
            {
                Type t when t == typeof(CommentNotFoundException) => new CommentNotFoundException(testCommentId),
                Type t when t == typeof(DeleteCommentFailedException) => new DeleteCommentFailedException(testCommentId),
                Type t when t == typeof(DbUpdateException) => new DbUpdateException(ResponseErrorMessages.DbUpdateException),
                Type t when t == typeof(OperationCanceledException) =>
                new OperationCanceledException(ResponseErrorMessages.OperationCanceledException),
                Type t when t == typeof(TimeoutException) => new TimeoutException(ResponseErrorMessages.TimeoutException),
                Type t when t == typeof(Exception) => new Exception(ResponseErrorMessages.UnexpectedErrorException),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            _factoryFixture.SetException(exception);

            var updatedComment = new EditCommentModel { Content = "Test comment" };
            var content = HttpHelper.GetJsonHttpContent(updatedComment);

            // Act
            var response = await _client.PostAsync(url, content);

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
