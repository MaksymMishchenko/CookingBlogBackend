using Microsoft.EntityFrameworkCore;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests
{
    public class CommentServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        public CommentServiceIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task SeedTestData(ApplicationDbContext context)
        {
            var post1 = new Post
            {
                Title = "Test Post",
                Description = "This is a test post.",
                Content = "Content of the test post.",
                ImageUrl = "http://example.com/image.jpg",
                MetaTitle = "Test Post Meta Title",
                MetaDescription = "Test Post Meta Description",
                Slug = "test-post-one",
                Comments = new List<Comment>()
            };

            var post2 = new Post
            {
                Title = "Test Post 2",
                Description = "This is a test post 2.",
                Content = "Content of the test post 2.",
                ImageUrl = "http://example.com/image2.jpg",
                MetaTitle = "Test Post Meta Title 2",
                MetaDescription = "Test Post Meta Description 2",
                Slug = "test-post-two",
                Comments = new List<Comment>()
            };

            await context.AddRangeAsync(post1, post2);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task AddCommentAsync_Should_Add_Comment_To_Specific_Post()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var commentService = new CommentService(context);
            await SeedTestData(context);

            var post = await context.Posts.FirstOrDefaultAsync(p => p.Slug == "test-post-one");
            Assert.NotNull(post);

            var postId = post.PostId;

            var comment = new Comment
            {
                Content = "Comment content for post 1",
                Author = "Bob",
                CreatedAt = DateTime.Now
            };

            // Act
            await commentService.AddCommentAsync(postId, comment);

            // Assert
            var updatedPost = await context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.PostId == postId);
            Assert.NotNull(post);
            Assert.Contains(updatedPost.Comments, c => c.Content == comment.Content
            && c.Author == comment.Author);
            Assert.Single(updatedPost.Comments);
        }

        [Fact]
        public async Task EditCommentAsync_Should_Edit_Comment_If_Exist()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var commentService = new CommentService(context);
            await SeedTestData(context);

            var post = await context.Posts.FirstOrDefaultAsync(p => p.Slug == "test-post-two");
            Assert.NotNull(post);
            var postId = post.PostId;

            var comment = new Comment
            {
                Content = "Comment content for post 2",
                Author = "William",
                CreatedAt = DateTime.Now
            };

            await commentService.AddCommentAsync(postId, comment);
            Assert.Contains(post.Comments, c => c.Content == comment.Content
            && c.Author == comment.Author);

            comment.Content = "Edited Comment content for post 2";

            // Act
            await commentService.EditCommentAsync(comment);

            // Assert
            var editedComment = await context.Comments.FirstOrDefaultAsync(c => c.Content == comment.Content);
            Assert.NotNull(editedComment);
            Assert.Equal(comment.Content, editedComment.Content);
        }

        [Fact]
        public async Task DeleteCommentAsync_Should_Remove_Comment_If_Exists()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var commentService = new CommentService(context);
            await SeedTestData(context);

            var post = await context.Posts.FirstOrDefaultAsync(p => p.Slug == "test-post-two");
            Assert.NotNull(post);
            var postId = post.PostId;

            var comment = new Comment
            {
                Content = "Comment content for post 2",
                Author = "William",
                CreatedAt = DateTime.Now
            };

            await commentService.AddCommentAsync(postId, comment);

            var initialCommentCount = await context.Comments.CountAsync();

            // Act
            await commentService.DeleteCommentAsync(comment.CommentId);

            // Assert
            var deletedComment = await context.Comments.FindAsync(comment.CommentId);
            var finalCommentCount = await context.Comments.CountAsync();
            Assert.Null(deletedComment);
            Assert.Equal(initialCommentCount - 1, finalCommentCount);
        }
    }
}

