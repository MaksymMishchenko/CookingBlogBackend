using Microsoft.EntityFrameworkCore;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests
{
    public class CommentServiceTests
    {
        private (CommentService commentService, ApplicationDbContext context) GetCommentService()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var dbContext = new ApplicationDbContext(options);
            var commentService = new CommentService(dbContext);

            return (commentService, dbContext);
        }

        [Fact]
        public async Task AddCommentAsync_Should_Add_Comment_With_CorrectPostId_And_CreatedAt()
        {
            // Arrange
            var (commentService, context) = GetCommentService();

            var postId = 1;
            var comment = new Comment { Content = "Some test comment" };

            // Act
            await commentService.AddCommentAsync(postId, comment);

            // Assert
            Assert.Equal(postId, comment.PostId);
            Assert.True(comment.CreatedAt <= DateTime.Now);

            var addedComment = await context.Comments
                .FirstOrDefaultAsync(c => c.PostId == postId && c.Content == "Some test comment");
            Assert.NotNull(addedComment);
            Assert.Equal("Some test comment", addedComment.Content);
            Assert.Equal(postId, addedComment.PostId);
        }

        [Fact]
        public async Task DeleteCommentAsync_Shoul_Delete_Comment_When_Exists()
        {
            // Arrange
            var (commentService, context) = GetCommentService();
            var comment = new Comment { CommentId = 1, Content = "Test content", PostId = 1 };

            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            // Act
            await commentService.DeleteCommentAsync(comment.CommentId);

            // Assert
            var detectedComment = await context.Comments.FindAsync(comment.CommentId);
            Assert.Null(detectedComment);
        }

        [Fact]
        public async Task DeleteCommentAsync_Should_NotFail_WhenComment_DoesNotExist()
        {
            // Arrange
            var (commentService, context) = GetCommentService();
            var commentId = 999;

            // Act
            await commentService.DeleteCommentAsync(commentId);

            // Assert
            Assert.True(true); // Dummy assertion since the main goal is to ensure no exception is thrown
        }

        [Fact]
        public async Task EditCommentAsync_Should_Edit_Comment_ById()
        {
            // Arrange
            var (commentService, context) = GetCommentService();
            var comment = new Comment { CommentId = 1, Content = "Test content", PostId = 1 };

            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var editedComment = new Comment { CommentId = 1, Content = "Test content", PostId = 1 };

            // Act
            await commentService.EditCommentAsync(editedComment);

            // Assert
            var fetchedComment = await context.Comments.FindAsync(comment.CommentId);
            Assert.NotNull(fetchedComment);
            Assert.Equal(comment.Content, fetchedComment.Content);
        }

        [Fact]
        public async Task EditCommentAsync_Should_NotUpdate_NotExistentComment()
        {
            // Arrange
            var (commentService, context) = GetCommentService();

            var nonExistentComment = new Comment { CommentId = 999, Content = "Non-existent content", PostId = 1 };

            // Act
            await commentService.EditCommentAsync(nonExistentComment);

            // Assert
            var fetchedComment = await context.Comments.FindAsync(nonExistentComment.CommentId);
            Assert.Null(fetchedComment);
        }
    }
}
