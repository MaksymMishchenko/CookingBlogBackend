using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PostApiService.Tests.IntegrationTests.Middlewares
{
    public class ContributorExceptionMiddlewareTests : IClassFixture<ContributorExceptionMiddlewareFixture>
    {
        private readonly HttpClient _client;
        private readonly ContributorExceptionMiddlewareFixture _factoryFixture;

        public ContributorExceptionMiddlewareTests(ContributorExceptionMiddlewareFixture factoryFixture)
        {
            _factoryFixture = factoryFixture;
            _client = _factoryFixture.Client;
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
