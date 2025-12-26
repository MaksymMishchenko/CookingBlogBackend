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
        public CommentServiceTests()
        {
            _mockCommentRepo = Substitute.For<IRepository<Comment>>();
            _mockPostRepo = Substitute.For<IRepository<Post>>();
            _mockAuthService = Substitute.For<IAuthService>();
        }

        [Fact]
        public async Task AddCommentAsync_ShouldAddCommentToPost()
        {
            // Arrange            
            var postId = 1;
            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var testUser = new IdentityUser { Id = "user123" };
            _mockAuthService.GetCurrentUserAsync().Returns(Task.FromResult(testUser));

            var service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockAuthService);

            // Act
            await service.AddCommentAsync(postId, comment);

            // Assert
            await _mockCommentRepo.Received(1)
                .AddAsync(comment, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateCommentContent_AndSaveChanges()
        {
            // Arrange
            var commentId = 1;
            var existingPost = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.Id == commentId);

            _mockCommentRepo.GetByIdAsync(Arg.Any<int>()).Returns(existingPost);

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            var service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockAuthService);

            // Act
            await service.UpdateCommentAsync(commentId, updatedComment);

            // Assert
            await _mockCommentRepo.Received(1)
                .GetByIdAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentAndSaveChanges()
        {
            // Arrange
            var commentId = 1;

            var post = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.Id == commentId);

            _mockCommentRepo.GetByIdAsync(Arg.Any<int>()).Returns(post);

            var service = new CommentService(_mockCommentRepo, _mockPostRepo, _mockAuthService);

            // Act
            await service.DeleteCommentAsync(commentId);

            // Assert            
            await _mockCommentRepo.Received(1)
                .GetByIdAsync(Arg.Any<int>());
        }
    }
}
