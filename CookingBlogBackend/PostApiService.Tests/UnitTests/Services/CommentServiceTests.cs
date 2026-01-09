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
        public CommentServiceTests()
        {
            _mockCommentRepo = Substitute.For<IRepository<Comment>>();
            _mockPostRepo = Substitute.For<IRepository<Post>>();
            _mockAuthService = Substitute.For<IAuthService>();
            _service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockAuthService);
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
        public async Task UpdateCommentAsync_ShouldUpdateCommentContent_AndSaveChanges()
        {
            // Arrange
            var commentId = 1;
            var existingPost = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.Id == commentId);

            _mockCommentRepo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(existingPost);

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            var service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockAuthService);

            // Act
            await service.UpdateCommentAsync(commentId, updatedComment);

            // Assert
            await _mockCommentRepo.Received(1)
                .GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentAndSaveChanges()
        {
            // Arrange
            var commentId = 1;

            var post = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.Id == commentId);

            _mockCommentRepo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(post);

            var service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockAuthService);

            // Act
            await service.DeleteCommentAsync(commentId);

            // Assert            
            await _mockCommentRepo.Received(1)
                .GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }
    }
}
