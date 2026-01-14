using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class CommentServiceTests
    {
        private readonly ICommentRepository _mockCommentRepo;
        private readonly IPostRepository _mockPostRepo;
        private readonly IWebContext _mockWebContext;
        private readonly CommentService _service;

        public CommentServiceTests()
        {
            _mockCommentRepo = Substitute.For<ICommentRepository>();
            _mockPostRepo = Substitute.For<IPostRepository>();
            _mockWebContext = Substitute.For<IWebContext>();

            _service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockWebContext);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            _mockWebContext.UserId.Returns(string.Empty);

            // Act 
            var result = await _service.AddCommentAsync(postId, "Valid comment content");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockCommentRepo.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnNotFoundResult_WhenPostDoesNotExist()
        {
            // Arrange
            const int postId = 1;
            const string validComment = "Test comment content";

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

            _mockPostRepo.IsAvailableForCommentingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(false);

            // Act
            var result = await _service.AddCommentAsync(postId, validComment);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(PostM.Errors.PostNotFound, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

            await _mockPostRepo.Received(1).IsAvailableForCommentingAsync
                (Arg.Any<int>(), Arg.Any<CancellationToken>());

            await _mockCommentRepo.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnOkResult_WhenValidDataProvided()
        {
            // Arrange            
            const int postId = 1;
            string validComment = "Valid Comment";
            const string userName = "Nick";
            const string userId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
            var token = CancellationToken.None;

            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.UserName.Returns(userName);
            _mockPostRepo.IsAvailableForCommentingAsync(Arg.Any<int>(), token)
                .Returns(true);

            // Act 
            var result = await _service.AddCommentAsync(postId, validComment);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var data = result.Value!;
            Assert.Equal(userName, data.Author);
            Assert.Equal(validComment, data.Content);
            Assert.NotEqual(default, data.CreatedAt);
            Assert.Equal(userId, data.UserId);
            Assert.Equal(CommentM.Success.CommentAddedSuccessfully, result.Message);

            await _mockPostRepo.Received(1).IsAvailableForCommentingAsync(Arg.Any<int>(), token);

            await _mockCommentRepo.Received(1).AddAsync(Arg.Is<Comment>(c =>
                c.Content == validComment &&
                c.PostId == postId &&
                c.UserId == userId &&
                c.CreatedAt != default),
                token);

            await _mockCommentRepo.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            _mockWebContext.UserId.Returns(string.Empty);

            // Act 
            var result = await _service.UpdateCommentAsync(postId, "Valid comment content");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockCommentRepo.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnNotFoundResult_WhenCommentDoesNotExist()
        {
            // Arrange
            const int commentId = 99999;
            const string validComment = "Test comment content";

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

            _mockCommentRepo.GetWithUserAsync(commentId, Arg.Any<CancellationToken>())
                .Returns((Comment)null!);

            // Act
            var result = await _service.UpdateCommentAsync(commentId, validComment);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(CommentM.Errors.NotFound, result.Message);
            Assert.Equal(CommentM.Errors.NotFoundCode, result.ErrorCode);

            await _mockCommentRepo.Received(1).GetWithUserAsync(commentId, Arg.Any<CancellationToken>());

            await _mockCommentRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnForbidden_WhenUserIsNotOwnerAndNotAdmin()
        {
            // Arrange
            var currentUserId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
            var ownerId = "3f2504e0-4f89-11d3-9a0c-0305e82c3302";
            var commentId = 10;
            const string hackComment = "<script>Hack content</script>";

            var existingComment = new Comment { Id = commentId, UserId = ownerId, User = new IdentityUser { Id = ownerId } };

            _mockWebContext.UserId.Returns(currentUserId);
            _mockCommentRepo.GetWithUserAsync(commentId, Arg.Any<CancellationToken>()).Returns(existingComment);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IpAddress.Returns("127.0.0.1");

            // Act
            var result = await _service.UpdateCommentAsync(commentId, hackComment);

            // Assert
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Equal(CommentM.Errors.AccessDenied, result.Message);
            Assert.Equal(CommentM.Errors.AccessDeniedCode, result.ErrorCode);

            await _mockCommentRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldSucceedAndMarkAsAdminEdited_WhenUserIsAdmin()
        {
            // Arrange
            const string adminId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
            const string ownerId = "3f2504e0-4f89-11d3-9a0c-0305e82c3302";
            var commentId = 10;
            var newContent = "Moderated content";

            var existingComment = new Comment
            {
                Id = commentId,
                UserId = ownerId,
                User = new IdentityUser { Id = ownerId, UserName = "Author" },
                IsEditedByAdmin = false
            };
            var adminUser = new IdentityUser { Id = adminId, UserName = "Admin" };

            _mockWebContext.UserId.Returns(adminId);
            _mockCommentRepo.GetWithUserAsync(commentId, Arg.Any<CancellationToken>()).Returns(existingComment);
            _mockWebContext.IsAdmin.Returns(true);
            _mockWebContext.IpAddress.Returns("127.0.0.1");

            // Act
            var result = await _service.UpdateCommentAsync(commentId, newContent);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(existingComment.IsEditedByAdmin);
            Assert.Equal(newContent, existingComment.Content);
            await _mockCommentRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldSucceed_WhenUserIsOwner()
        {
            // Arrange
            const string userId = "3f2504e0-4f89-11d3-9a0c-0305e82c3302";
            const int commentId = 10;
            var newContent = "My updated comment";

            var currentUser = new IdentityUser { Id = userId, UserName = "Owner" };
            var existingComment = new Comment
            {
                Id = commentId,
                UserId = userId,
                User = currentUser,
                IsEditedByAdmin = false
            };

            _mockWebContext.UserId.Returns(userId);
            _mockCommentRepo.GetWithUserAsync(commentId, Arg.Any<CancellationToken>()).Returns(existingComment);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IpAddress.Returns("127.0.0.1");

            // Act
            var result = await _service.UpdateCommentAsync(commentId, newContent);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newContent, existingComment.Content);
            Assert.False(existingComment.IsEditedByAdmin);
            await _mockCommentRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange            
            const int commentId = 1;
            _mockWebContext.UserId.Returns(string.Empty);

            // Act 
            var result = await _service.DeleteCommentAsync(commentId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockCommentRepo.DidNotReceive().DeleteAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnNotFoundResult_WhenCommentDoesNotExist()
        {
            // Arrange
            const string userId = "3f2504e0-4f89-11d3-9a0c-0305e82c3302";
            const int invalidCommentId = 99999;

            _mockWebContext.UserId.Returns(userId);

            _mockCommentRepo.GetByIdAsync(invalidCommentId, Arg.Any<CancellationToken>())
                .Returns((Comment)null!);

            // Act
            var result = await _service.DeleteCommentAsync(invalidCommentId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(CommentM.Errors.NotFound, result.Message);
            Assert.Equal(CommentM.Errors.NotFoundCode, result.ErrorCode);

            await _mockCommentRepo.Received(1)
                .GetByIdAsync(invalidCommentId, Arg.Any<CancellationToken>());

            await _mockCommentRepo.DidNotReceive()
                .DeleteAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            const string currentUserId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
            const string ownerId = "3f2504e0-4f89-11d3-9a0c-0305e82c3302";
            var commentId = 10;

            var existingComment = new Comment { Id = commentId, UserId = ownerId };
            var currentUser = new IdentityUser { Id = currentUserId };

            _mockCommentRepo.GetByIdAsync(commentId, Arg.Any<CancellationToken>())
                .Returns(existingComment);
            _mockWebContext.UserId.Returns(currentUserId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IpAddress.Returns("127.0.0.1");

            // Act
            var result = await _service.DeleteCommentAsync(commentId);

            // Assert
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Equal(CommentM.Errors.AccessDenied, result.Message);
            Assert.Equal(CommentM.Errors.AccessDeniedCode, result.ErrorCode);

            await _mockCommentRepo.DidNotReceive().DeleteAsync(Arg.Any<Comment>(),
                Arg.Any<CancellationToken>());
            await _mockCommentRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnSuccess_WhenUserIsNotOwnerButIsAdmin()
        {
            // Arrange
            const string adminId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
            const string ownerId = "3f2504e0-4f89-11d3-9a0c-0305e82c3302";
            var commentId = 10;
            var token = CancellationToken.None;

            var existingComment = new Comment { Id = commentId, UserId = ownerId };

            _mockCommentRepo.GetByIdAsync(commentId, token)
                .Returns(existingComment);

            _mockWebContext.UserId.Returns(adminId);
            _mockWebContext.IsAdmin.Returns(true);
            _mockWebContext.IpAddress.Returns("127.0.0.1");

            // Act
            var result = await _service.DeleteCommentAsync(commentId, token);

            // Assert
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(CommentM.Success.CommentDeletedSuccessfully, result.Message);

            await _mockCommentRepo.Received(1).DeleteAsync(existingComment, token);
            await _mockCommentRepo.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnSuccess_WhenUserIsOwner()
        {
            // Arrange
            const string userId = "3f2504e0-4f89-11d3-9a0c-0305e82c3302";
            var commentId = 1;
            var ct = CancellationToken.None;

            var existingComment = new Comment { Id = commentId, UserId = userId };

            _mockCommentRepo.GetByIdAsync(commentId, ct).Returns(existingComment);
            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.IsAdmin.Returns(false);

            // Act
            var result = await _service.DeleteCommentAsync(commentId, ct);

            // Assert            
            Assert.True(result.IsSuccess);
            Assert.Equal(CommentM.Success.CommentDeletedSuccessfully, result.Message);

            await _mockCommentRepo.Received(1).DeleteAsync(existingComment, ct);
            await _mockCommentRepo.Received(1).SaveChangesAsync(ct);
        }
    }
}
