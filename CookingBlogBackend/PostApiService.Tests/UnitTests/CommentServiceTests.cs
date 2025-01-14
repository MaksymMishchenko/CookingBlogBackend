using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;
using System;

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
                      
            _mockContext.Setup(c => c.Posts).ReturnsDbSet(DataFixture.GetTestEmptyPostsList());
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

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

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(DataFixture.GetTestListPosts());
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(DataFixture.GetTestListComments());
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

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(DataFixture.GetTestListPosts());
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(DataFixture.GetTestListComments());
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act
            var result = await _commentService.AddCommentAsync(postId, comment);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task DeleteCommentAsync_ShouldThrowArgumentException_WhenCommentIdIsInvalid(int commentId)
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            var newComment = new Comment { Content = "Some test comment", Author = "Test author" };

            context.Comments.Add(newComment);
            await context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => commentService.DeleteCommentAsync(commentId));
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnTrue_WhenCommentRemoved()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            var commentId = 1;
            var newComment = new Comment { Content = "Some test comment", Author = "Test author" };

            context.Comments.Add(newComment);
            await context.SaveChangesAsync();

            // Act
            var result = await commentService.DeleteCommentAsync(commentId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnFalse_IfCommentDoesNotExist()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            var nonExistentCommentId = 999;

            // Act
            var result = await commentService.DeleteCommentAsync(nonExistentCommentId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldReturnTrue_IfCommentEdited()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            var commentId = 1;
            var newComment = new Comment { Content = "Original comment", Author = "Test author" };
            context.Comments.Add(newComment);
            await context.SaveChangesAsync();

            newComment.Content = "Edited comment";

            // Act
            var result = await commentService.EditCommentAsync(newComment);

            // Assert
            Assert.True(result);
            var editedComment = await context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.Content == "Edited comment");
            Assert.Equal(newComment.Content, editedComment.Content);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldReturnFalse_WhenCommentDoesNotExist()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            var nonExistentCommentId = 999;
            var nonExistentComment = new Comment
            {
                CommentId = nonExistentCommentId,
                Content = "Test comment",
                Author = "Test author"
            };

            // Act
            var result = await commentService.EditCommentAsync(nonExistentComment);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldThrowArgumentNullException_WhenCommentIsNull()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => commentService.EditCommentAsync(null));
            Assert.Equal("Comment cannot be null (Parameter 'comment')", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EditCommentAsync_ShouldThrowArgumentException_WhenCommentContentIsNull(string content)
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            var newComment = new Comment { Content = content, Author = "Test author" };

            // Act & Assert

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => commentService.EditCommentAsync(newComment));
            Assert.Equal("Comment content cannot be null or empty. (Parameter 'Content')", exception.Message);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldThrowArgumentException_WhenCommentIdIsInvalid()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var commentService = new CommentService(context, logger);

            var invalidCommentid = -1;
            var newComment = new Comment { CommentId = invalidCommentid, Content = "Test comment", Author = "Test author" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => commentService.EditCommentAsync(newComment));
            Assert.Equal("Invalid comment ID. (Parameter 'CommentId')", exception.Message);
        }
    }
}
