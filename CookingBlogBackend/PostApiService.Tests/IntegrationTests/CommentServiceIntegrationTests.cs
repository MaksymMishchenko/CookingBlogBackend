using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests
{
    public class CommentServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;        
        public CommentServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        private CommentService CreateCommentService()
        {
            var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            return new CommentService(context, logger);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldAddNewCommentToPostSuccessfully()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var commentService = CreateCommentService();

            int postId = 1;
            var comment = new Comment
            {
                PostId = postId,
                Content = "Test comment from Bob",
                Author = "Bob"
            };

            // Act
            await commentService.AddCommentAsync(postId, comment);

            // Assert
            var addedComment = await context.Comments
                .FirstOrDefaultAsync(c => c.Content == comment.Content
                && c.Author == comment.Author);
            Assert.NotNull(addedComment);
            Assert.Equal(postId, addedComment.PostId);
            var commentCount = await context.Comments.CountAsync(c => c.PostId == postId);
            Assert.Equal(4, commentCount);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateContentOfExistingComment()
        {
            // Arrange            
            using var context = _fixture.CreateContext();
            var commentService = CreateCommentService();

            int commentId = 2;
            var comment = new EditCommentModel
            {
                Content = "Edited comment content"
            };

            // Act
            await commentService.UpdateCommentAsync(commentId, comment);

            // Assert
            var editedComment = await context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            Assert.NotNull(editedComment);
            Assert.Equal(comment.Content, editedComment.Content);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentFromDataBase()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var commentService = CreateCommentService();

            var commentIdToRemove = 1; // Один із коментарів, засіяних у фікстурі
            var initialCount = await context.Comments.CountAsync();

            // Act
            await commentService.DeleteCommentAsync(commentIdToRemove);
            var finalCount = await context.Comments.CountAsync();

            // Assert
            var removedComment = await context.Comments.FindAsync(commentIdToRemove);
            Assert.Null(removedComment);
            Assert.Equal(initialCount - 1, finalCount);
        }
    }
}

