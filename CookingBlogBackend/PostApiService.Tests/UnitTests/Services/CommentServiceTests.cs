using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class CommentServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly IdentityUser _testUser;
        private readonly CommentService _commentService;

        public CommentServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
            _mockContext = new Mock<IApplicationDbContext>();
            _mockAuthService = new Mock<IAuthService>();
            _testUser = new IdentityUser { Id = "user123", UserName = "testuser", Email = "test@test.com" };

            _mockAuthService.Setup(auth => auth.GetCurrentUserAsync()).ReturnsAsync(_testUser);

            _commentService = new CommentService(_mockContext.Object,
                _mockAuthService.Object);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldAddCommentToPost()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var commentService = new CommentService(context, _mockAuthService.Object);
            var postId = 1;
            var initialCount = await context.Comments.CountAsync();

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act
            await commentService.AddCommentAsync(postId, comment);

            // Assert
            var totalCount = await context.Comments.CountAsync();
            Assert.Equal(initialCount + 1, totalCount);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateCommentContent_AndSaveChanges()
        {
            // Arrange
            var commentId = 1;
            var existingPost = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId);
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(existingPost);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act
            await _commentService.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            _mockContext.Verify(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentAndSaveChanges()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            // Act
            await _commentService.DeleteCommentAsync(commentId);

            // Assert            
            _mockContext.Verify(c => c.Comments.Remove(It.Is<Comment>(c => c.CommentId == commentId)), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
