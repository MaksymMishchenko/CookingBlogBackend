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

        //[Fact]
        //public async Task DeleteCommentAsync_Should_Remove_Comment_If_Exists()
        //{
        //    // Arrange
        //    using var context = _fixture.CreateContext();
        //    var postService = new PostService(context);
        //    var commentService = new CommentService(context);

        //    var postId = 1;
        //    var post = CreateTestPost(
        //        "Origin Post",
        //        "Origin Content",
        //        "Origin Author",
        //        "Origin Description",
        //        "origin-image.jpg",
        //        "origin-post",
        //        "Origin Post meta title",
        //        "Origin Post meta description"
        //        );

        //    await postService.AddPostAsync(post);

        //    var comment = new Comment
        //    {
        //        Content = "Comment to be deleted",
        //        Author = "William",
        //        CreatedAt = DateTime.Now
        //    };

        //    await commentService.AddCommentAsync(postId, comment);

        //    // Act
        //    await commentService.DeleteCommentAsync(comment.CommentId);

        //    // Assert
        //    var removedComment = await context.Comments.FindAsync(comment.CommentId);
        //    Assert.Null(removedComment);
        //}        
    }
}

