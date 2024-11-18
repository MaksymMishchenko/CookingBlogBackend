using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostApiService.Models;
using PostApiService.Services;
using PostApiService.Tests.Fixtures;

namespace PostApiService.Tests.UnitTests
{
    public class CommentServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private ApplicationDbContext _dbContext;
        private CommentService _commentService;
        private ILogger<CommentService> _logger;

        public CommentServiceTests(InMemoryDatabaseFixture fixture)
        {
            _dbContext = fixture.GetContext;
            _logger = new LoggerFactory().CreateLogger<CommentService>();
            _commentService = new CommentService(_dbContext, _logger);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnTrue_WhenPostExists()
        {
            // Arrange            
            var postId = 1;

            _dbContext.Posts.Add(new Post
            {
                PostId = postId,
                Title = "Test post",
                Content = "Test post content",
                Author = "Test author",
                ImageUrl = "image.jpg",
                Slug = "test-slug",
                MetaTitle = "Test meta title",
                MetaDescription = "Tets meta descr"
            });
            await _dbContext.SaveChangesAsync();

            var comment = new Comment { Content = "Some test comment", Author = "Test author" };

            // Act
            var result = await _commentService.AddCommentAsync(postId, comment);

            // Assert
            Assert.True(result);
            Assert.Single(await _dbContext.Comments.ToListAsync());

            var addedComment = await _dbContext.Comments
                .FirstOrDefaultAsync(c => c.PostId == postId && c.Content == "Some test comment");
            Assert.NotNull(addedComment);
            Assert.Equal("Some test comment", addedComment.Content);
            Assert.Equal(comment.Content, addedComment.Content);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldThrowKeyNotFoundException_WhenPostDoesNotExist()
        {
            // Arrange                       
            var postId = 1;
            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _commentService.AddCommentAsync(postId, comment));
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnTrue_WhenCommentRemoved()
        {
            // Arrange            
            var commentId = 1;
            var newComment = new Comment { Content = "Some test comment", Author = "Test author" };

            _dbContext.Comments.Add(newComment);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _commentService.DeleteCommentAsync(commentId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnFalse_IfCommentDoesNotExist()
        {
            // Arrange            
            var nonExistentCommentId = 999;

            // Act
            var result = await _commentService.DeleteCommentAsync(nonExistentCommentId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldReturnTrue_IfCommentEdited()
        {
            // Arrange
            var commentId = 1;

            var newComment = new Comment { Content = "Original comment", Author = "Test author" };
            _dbContext.Comments.Add(newComment);
            await _dbContext.SaveChangesAsync();

            newComment.Content = "Edited comment";

            // Act
            var result = await _commentService.EditCommentAsync(newComment);

            // Assert

            Assert.True(result);
            var editedComment = await _dbContext.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.Content == "Edited comment");
            Assert.Equal(newComment.Content, editedComment.Content);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldReturnFalse_WhenCommentDoesNotExist()
        {
            // Arrange
            var nonExistentCommentId = 999;
            var nonExistentComment = new Comment
            {
                CommentId = nonExistentCommentId,
                Content = "Test comment",
                Author = "Test author"
            };

            // Act
            var result = await _commentService.EditCommentAsync(nonExistentComment);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldThrowArgumentNullException_WhenCommentIsNull()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _commentService.EditCommentAsync(null));
            Assert.Equal("Comment cannot be null (Parameter 'comment')", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EditCommentAsync_ShouldThrowArgumentException_WhenCommentContentIsNull(string content)
        {
            // Arrange
            var newComment = new Comment { Content = content, Author = "Test author" };

            // Act & Assert

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _commentService.EditCommentAsync(newComment));
            Assert.Equal("Comment content cannot be null or empty. (Parameter 'Content')", exception.Message);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldThrowArgumentException_WhenCommentIdIsInvalid()
        {
            // Arrange
            var invalidCommentid = -1;
            var newComment = new Comment { CommentId = invalidCommentid, Content = "Test comment", Author = "Test author" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _commentService.EditCommentAsync(newComment));
            Assert.Equal("Invalid comment ID. (Parameter 'CommentId')", exception.Message);
        }
    }
}
