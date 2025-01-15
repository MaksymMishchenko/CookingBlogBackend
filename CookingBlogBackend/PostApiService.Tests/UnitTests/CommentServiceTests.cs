using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests
{
    public class CommentServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private InMemoryDatabaseFixture _fixture;
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<CommentService>> _mockLoggerService;
        private readonly CommentService _commentService;

        public CommentServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLoggerService = new Mock<ILogger<CommentService>>();
            _commentService = new CommentService(_mockContext.Object, _mockLoggerService.Object);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldThrowKeyNotFoundException_IfPostDoesNotExist()
        {
            // Arrange
            var postId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(DataFixture.GetEmptyPostList());

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _commentService.AddCommentAsync(postId, comment));
            Assert.Equal($"Post with ID {postId} does not exist.", exception.Message);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnTrue_IfCommentAddedSuccessfully()
        {
            // Arrange
            var postId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(DataFixture.GetListWithPost());
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(DataFixture.GetEmptyCommentList());
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act
            var result = await _commentService.AddCommentAsync(postId, comment);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnFalse_WhenSaveChangesFails()
        {
            // Arrange
            var postId = 1;
            var saveChangedResult = 0;

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(DataFixture.GetListWithPost());
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(DataFixture.GetEmptyCommentList());
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act
            var result = await _commentService.AddCommentAsync(postId, comment);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var postId = 1;

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(DataFixture.GetListWithPost());
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(DataFixture.GetEmptyCommentList());
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception()); ;

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _commentService.AddCommentAsync(postId, comment));
            Assert.Equal("An unexpected error occurred. Please try again later.", exception.Message);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldThrowInvalidOperationException_IfCommentDoesNotExist()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Comments).ReturnsDbSet(DataFixture.GetListWithComment());

            var comment = new EditCommentModel { Content = "Updated comment" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _commentService.UpdateCommentAsync(commentId, comment));

            Assert.Equal($"Comment with ID {commentId} does not exist", exception.Message);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnTrue_IfCommentAddedSuccessfully()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(DataFixture.GetListWithComment()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act
            var result = await _commentService.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnFalse_WhenSaveChangesFails()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 0;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(DataFixture.GetListWithComment()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act
            var result = await _commentService.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldThrowDbUpdateConcurrencyException_WhenDbSaveFailsDueToConcurrency()
        {
            // Arrange
            var commentId = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(DataFixture.GetListWithComment()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act & Assert            
            var concurrencyException = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _commentService
            .UpdateCommentAsync(commentId, updatedComment));

            Assert.Equal($"Concurrency issue while updating comment ID {commentId}.", concurrencyException.Message);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var commentId = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(DataFixture.GetListWithComment()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act & Assert            
            var concurrencyException = await Assert.ThrowsAsync<Exception>(() => _commentService
            .UpdateCommentAsync(commentId, updatedComment));

            Assert.Equal("An unexpected error occurred. Please try again later.", concurrencyException.Message);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldThrowKeyNotFoundException_IfPostDoesNotExist()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Comments).ReturnsDbSet(DataFixture.GetEmptyCommentList());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _commentService.DeleteCommentAsync(commentId));

            Assert.Equal($"Comment with ID {commentId} does not exist", exception.Message);
        }
    }
}
