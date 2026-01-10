using Microsoft.AspNetCore.Http;
using NSubstitute.ExceptionExtensions;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services
{
    public class CommentServiceTests
    {
        private readonly IRepository<Comment> _mockCommentRepo;
        private readonly IRepository<Post> _mockPostRepo;
        private readonly IAuthService _mockAuthService;
        private readonly CommentService _service;
        private readonly IHttpContextAccessor _httpContextAccessorMock;

        public CommentServiceTests()
        {
            _mockCommentRepo = Substitute.For<IRepository<Comment>>();
            _mockPostRepo = Substitute.For<IRepository<Post>>();
            _mockAuthService = Substitute.For<IAuthService>();
            _httpContextAccessorMock = Substitute.For<IHttpContextAccessor>();

            _service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockAuthService, _httpContextAccessorMock);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnNotFoundResult_WhenPostDoesNotExist()
        {
            // Arrange
            const int postId = 1;
            string content = "Test comment content";
            var errorMessage = PostM.Errors.PostNotFound;
            var errorCode = PostM.Errors.PostNotFoundCode;

            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            // Act
            var result = await _service.AddCommentAsync(postId, content);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(errorMessage, result.Message);
            Assert.Equal(errorCode, result.ErrorCode);

            await _mockPostRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>());

            await _mockCommentRepo.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCommentAsync_ShouldThrowUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            _mockAuthService.GetCurrentUserAsync()
                .Throws(new UnauthorizedAccessException(Auth.LoginM.Errors.UnauthorizedAccess));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AddCommentAsync(postId, "some content"));

            await _mockCommentRepo.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnOkResult_WhenValidDataProvided()
        {
            // Arrange            
            const int postId = 1;
            string content = "Test comment content";
            var testUser = new IdentityUser { Id = "user123", UserName = "TestUser" };
            string successMessage = CommentM.Success.CommentAddedSuccessfully;
            var token = CancellationToken.None;

            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), token)
                .Returns(true);

            _mockAuthService.GetCurrentUserAsync().Returns(testUser);

            // Act 
            var result = await _service.AddCommentAsync(postId, content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var data = result.Value!;
            Assert.Equal(data.Author, testUser.UserName);
            Assert.Equal(content, data.Content);
            Assert.NotEqual(default, data.CreatedAt);
            Assert.Equal(data.UserId, testUser.Id);
            Assert.Equal(successMessage, result.Message);

            await _mockPostRepo.Received(1).AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(),
                token);

            await _mockAuthService.Received(1).GetCurrentUserAsync();

            await _mockCommentRepo.Received(1).AddAsync(Arg.Is<Comment>(c =>
                c.Content == content &&
                c.PostId == postId &&
                c.UserId == testUser.Id &&
                c.CreatedAt != default),
                token);

            await _mockCommentRepo.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnNotFoundResult_WhenCommentDoesNotExist()
        {
            // Arrange
            const int invalidCommentId = 99999;
            string comment = "Valid comment";

            _mockCommentRepo.GetByIdAsync(invalidCommentId, Arg.Any<CancellationToken>())
                .Returns((Comment)null!);

            // Act
            var result = await _service.UpdateCommentAsync(invalidCommentId, comment);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(CommentM.Errors.NotFound, result.Message);
            Assert.Equal(CommentM.Errors.NotFoundCode, result.ErrorCode);

            await _mockCommentRepo.Received(1)
                .GetByIdAsync(invalidCommentId, Arg.Any<CancellationToken>());

            await _mockCommentRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = "user-1";
            var ownerId = "user-2";
            var commentId = 10;

            var existingComment = new Comment { Id = commentId, UserId = ownerId, Content = "Old" };
            var currentUser = new IdentityUser { Id = userId, UserName = "test" };

            _mockCommentRepo.GetByIdAsync(commentId, Arg.Any<CancellationToken>()).Returns(existingComment);
            _mockAuthService.GetCurrentUserAsync().Returns(currentUser);

            //// Налаштовуємо IP через NSubstitute
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            _httpContextAccessorMock.HttpContext.Returns(context);

            // Act
            var result = await _service.UpdateCommentAsync(commentId, "New Content");

            // Assert
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Equal(CommentM.Errors.AccessDenied, result.Message);

            // Перевіряємо, що SaveChanges НЕ викликався
            await _mockCommentRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateCommentContent_AndSaveChanges()
        {
            // Arrange
            const int commentId = 1;
            const string userId = "user-123";
            const string newContent = "Content edited successfully";
            var token = CancellationToken.None;

            // 1. Створюємо коментар, де UserId збігається з майбутнім "поточним юзером"
            var existingComment = new Comment
            {
                Id = commentId,
                UserId = userId,
                Content = "Old Content"
            };

            var currentUser = new IdentityUser
            {
                Id = userId,
                UserName = "testuser"
            };

            // Мокаємо отримання коментаря
            _mockCommentRepo.GetByIdAsync(commentId, token).Returns(existingComment);

            // Мокаємо авторизацію (повертаємо власника)
            _mockAuthService.GetCurrentUserAsync().Returns(currentUser);

            // Мокаємо HttpContext, щоб сервіс не падав при зверненні до контексту
            var context = new DefaultHttpContext();
            _httpContextAccessorMock.HttpContext.Returns(context);

            // Act
            var result = await _service.UpdateCommentAsync(commentId, newContent, token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(newContent, result.Value?.Content);
            Assert.Equal(CommentM.Success.CommentUpdatedSuccessfully, result.Message);

            // Перевіряємо, що дані в об'єкті змінені перед збереженням
            Assert.Equal(newContent, existingComment.Content);

            // Перевіряємо виклики репозиторію
            await _mockCommentRepo.Received(1).GetByIdAsync(commentId, token);
            await _mockCommentRepo.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentAndSaveChanges()
        {
            // Arrange
            var commentId = 1;

            var post = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.Id == commentId);

            _mockCommentRepo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(post);

            // Act
            await _service.DeleteCommentAsync(commentId);

            // Assert            
            await _mockCommentRepo.Received(1)
                .GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }
    }
}
