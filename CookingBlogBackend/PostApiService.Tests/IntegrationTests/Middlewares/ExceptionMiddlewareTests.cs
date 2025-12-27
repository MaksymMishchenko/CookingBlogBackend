using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models.Dto;
using System.Net;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Security.Claims;

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
        [InlineData(typeof(UserAlreadyExistsException), Authentication.Register, HttpStatusCode.Conflict)]
        [InlineData(typeof(EmailAlreadyExistsException), Authentication.Register, HttpStatusCode.Conflict)]
        [InlineData(typeof(UserClaimException), Authentication.Register, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(UserCreationException), Authentication.Register, HttpStatusCode.InternalServerError)]
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
                        .Returns(Task.FromException(new UserAlreadyExistsException(Auth.Registration.Errors.UsernameAlreadyExists)));
                    break;
                case Type t when t == typeof(EmailAlreadyExistsException):
                    authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new EmailAlreadyExistsException(Auth.Registration.Errors.EmailAlreadyExists)));
                    break;
                case Type t when t == typeof(UserClaimException):
                    authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
                        .Returns(Task.FromException(new UserClaimException(Auth.Registration.Errors.CreationFailed)));
                    break;
                case Type t when t == typeof(UserCreationException):
                    authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
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
                Type t when t == typeof(UserCreationException) => Auth.Registration.Errors.CreationFailed
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(AuthenticationException), Authentication.Login, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(UnauthorizedAccessException), Authentication.Login, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(UserNotFoundException), Authentication.Login, HttpStatusCode.Unauthorized)]
        [InlineData(typeof(ArgumentException), Authentication.Login, HttpStatusCode.InternalServerError)]
        public async Task LoginUser_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            var authServiceMock = _factoryFixture?.Services?.GetRequiredService<IAuthService>();

            authServiceMock.ClearReceivedCalls();

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
                Type t when t == typeof(Exception) => Global.Validation.UnexpectedErrorException
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(OperationCanceledException), Posts.Base, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), Posts.Base, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), Posts.Base, HttpStatusCode.InternalServerError)]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnExpectedStatusCode_WhenExceptionThrown
            (Type exceptionType, string url, HttpStatusCode expectedStatus)
        {
            // Arrange
            var postServiceMock = _factoryFixture?.Services?.GetRequiredService<IPostService>();

            postServiceMock.ClearReceivedCalls();

            Exception exceptionToThrow = exceptionType switch
            {
                Type t when t == typeof(OperationCanceledException) =>
                    new OperationCanceledException(Global.System.RequestCancelled),
                Type t when t == typeof(TimeoutException) =>
                    new TimeoutException(Global.System.Timeout),
                Type t when t == typeof(Exception) =>
                    new Exception(Global.System.DatabaseCriticalError),
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
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{Posts.Base}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(OperationCanceledException), $"{Posts.Base}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{Posts.Base}/2", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{Posts.Base}/3", HttpStatusCode.InternalServerError)]
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
                    Task.FromException<Post>(new OperationCanceledException(Global.System.RequestCancelled)),
                Type t when t == typeof(TimeoutException) =>
                    Task.FromException<Post>(new TimeoutException(Global.System.Timeout)),
                Type t when t == typeof(Exception) =>
                    Task.FromException<Post>(new Exception(Global.System.DatabaseCriticalError)),
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
                Type t when t == typeof(PostNotFoundException) => string.Format(PostM.Errors.PostNotFound, invalidPostId),
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostAlreadyExistException), Posts.Base, HttpStatusCode.Conflict)]
        [InlineData(typeof(AddPostFailedException), Posts.Base, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), Posts.Base, HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), Posts.Base, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), Posts.Base, HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), Posts.Base, HttpStatusCode.InternalServerError)]
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
                    Task.FromException<Post>(new DbUpdateException(Global.System.DbUpdateError)),
                Type t when t == typeof(OperationCanceledException) =>
                    Task.FromException<Post>(new OperationCanceledException(Global.System.RequestCancelled)),
                Type t when t == typeof(TimeoutException) =>
                    Task.FromException<Post>(new TimeoutException(Global.System.Timeout)),
                Type t when t == typeof(Exception) =>
                    Task.FromException<Post>(new Exception(Global.System.DatabaseCriticalError)),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
            };

            postServiceMock?.AddPostAsync(Arg.Any<Post>())
                .Returns(failedTask);

            var categories = TestDataHelper.GetCulinaryCategories();
            var newPost = TestDataHelper.GetSinglePost(categories);
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
                (PostM.Errors.PostAlreadyExist, postTitle),
                Type t when t == typeof(AddPostFailedException) => string.Format
                (PostM.Errors.AddPostFailed, postTitle),
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{Posts.Base}/1", HttpStatusCode.NotFound)]
        [InlineData(typeof(UpdatePostFailedException), $"{Posts.Base}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{Posts.Base}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{Posts.Base}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{Posts.Base}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{Posts.Base}/1", HttpStatusCode.InternalServerError)]
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
                        .Returns(Task.FromException<Post>(new DbUpdateException(Global.System.DbUpdateError)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new OperationCanceledException(Global.System.RequestCancelled)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new TimeoutException(Global.System.Timeout)));
                    break;
                case Type t when t == typeof(Exception):
                    postServiceMock?.UpdatePostAsync(Arg.Any<int>(), Arg.Any<Post>())
                        .Returns(Task.FromException<Post>(new Exception(Global.System.DatabaseCriticalError)));
                    break;
                default:
                    throw new ArgumentException($"Unsupported exception type: {exceptionType}");
            }

            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);
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
                (PostM.Errors.PostNotFound, testPostId),
                Type t when t == typeof(UpdatePostFailedException) => string.Format
                (PostM.Errors.UpdatePostFailed, postTitle),
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{Posts.Base}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(DeletePostFailedException), $"{Posts.Base}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{Posts.Base}/2", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{Posts.Base}/3", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{Posts.Base}/4", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{Posts.Base}/5", HttpStatusCode.InternalServerError)]
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
                        .Returns(Task.FromException(new DbUpdateException(Global.System.DbUpdateError)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new OperationCanceledException(Global.System.RequestCancelled)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new TimeoutException(Global.System.Timeout)));
                    break;
                case Type t when t == typeof(Exception):
                    postServiceMock?.DeletePostAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new Exception(Global.System.DatabaseCriticalError)));
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
                (PostM.Errors.PostNotFound, testPostId),
                Type t when t == typeof(DeletePostFailedException) => string.Format
                (PostM.Errors.DeletePostFailed, testPostId),
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(PostNotFoundException), $"{Comments.Base}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(AddCommentFailedException), $"{Comments.Base}/999", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{Comments.Base}/999", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{Comments.Base}/999", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{Comments.Base}/999", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{Comments.Base}/999", HttpStatusCode.InternalServerError)]
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
                        .Returns(Task.FromException(new DbUpdateException(Global.System.DbUpdateError)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new OperationCanceledException(Global.System.RequestCancelled)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new TimeoutException(Global.System.Timeout)));
                    break;
                case Type t when t == typeof(Exception):
                    commentServiceMock?.AddCommentAsync(Arg.Any<int>(), Arg.Any<Comment>())
                        .Returns(Task.FromException(new Exception(Global.System.DatabaseCriticalError)));
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
                (PostM.Errors.PostNotFound, testPostId),
                Type t when t == typeof(AddCommentFailedException) => string.Format
                (CommentM.Errors.AddCommentFailed, testPostId),
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(CommentNotFoundException), $"{Comments.Base}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(UpdateCommentFailedException), $"{Comments.Base}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{Comments.Base}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{Comments.Base}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{Comments.Base}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{Comments.Base}/1", HttpStatusCode.InternalServerError)]
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
                        .Returns(Task.FromException(new DbUpdateException(Global.System.DbUpdateError)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new OperationCanceledException(Global.System.RequestCancelled)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new TimeoutException(Global.System.Timeout)));
                    break;
                case Type t when t == typeof(Exception):
                    commentServiceMock?.UpdateCommentAsync(Arg.Any<int>(), Arg.Any<EditCommentModel>())
                        .Returns(Task.FromException(new Exception(Global.System.DatabaseCriticalError)));
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
                (CommentM.Errors.CommentNotFound, testCommentId),
                Type t when t == typeof(UpdateCommentFailedException) => string.Format
                (CommentM.Errors.UpdateCommentFailed, testCommentId),
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }

        [Theory]
        [InlineData(typeof(CommentNotFoundException), $"{Comments.Base}/999", HttpStatusCode.NotFound)]
        [InlineData(typeof(DeleteCommentFailedException), $"{Comments.Base}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(DbUpdateException), $"{Comments.Base}/1", HttpStatusCode.InternalServerError)]
        [InlineData(typeof(OperationCanceledException), $"{Comments.Base}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(TimeoutException), $"{Comments.Base}/1", HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(Exception), $"{Comments.Base}/1", HttpStatusCode.InternalServerError)]
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
                        .Returns(Task.FromException(new DbUpdateException(Global.System.DbUpdateError)));
                    break;
                case Type t when t == typeof(OperationCanceledException):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                         .Returns(Task.FromException(new OperationCanceledException(Global.System.RequestCancelled)));
                    break;
                case Type t when t == typeof(TimeoutException):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                        .Returns(Task.FromException(new TimeoutException(Global.System.Timeout)));
                    break;
                case Type t when t == typeof(Exception):
                    commentServiceMock?.DeleteCommentAsync(Arg.Any<int>())
                         .Returns(Task.FromException(new Exception(Global.System.DatabaseCriticalError)));
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
                (CommentM.Errors.CommentNotFound, testCommentId),
                Type t when t == typeof(DeleteCommentFailedException) => string.Format
                (CommentM.Errors.DeleteCommentFailed, testCommentId),
                Type t when t == typeof(DbUpdateException) => Global.System.DbUpdateError,
                Type t when t == typeof(OperationCanceledException) => Global.System.RequestCancelled,
                Type t when t == typeof(TimeoutException) => Global.System.Timeout,
                Type t when t == typeof(Exception) => Global.System.DatabaseCriticalError
            };

            Assert.Equal(expectedMessage, errorResponse.Message);
        }
    }
}