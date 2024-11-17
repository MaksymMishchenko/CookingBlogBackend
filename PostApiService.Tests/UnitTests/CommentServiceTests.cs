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
    }
}
