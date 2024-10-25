using Microsoft.EntityFrameworkCore;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests
{
    public class CommentServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly ApplicationDbContext _context;
        private PostService _postService;
        private CommentService _commentService;
        public CommentServiceIntegrationTests(IntegrationTestFixture fixture)
        {
            _context = fixture.Context;
            _postService = new PostService(_context);
            _commentService = new CommentService(_context);
        }

        private async Task SeedTestData()
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

            await _postService.AddPostAsync(post1);
            await _postService.AddPostAsync(post2);
        }

        [Fact]
        public async Task AddCommentAsync_Should_Add_Comment_To_Specific_Post()
        {
            // Arrange
            await SeedTestData();

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Slug == "test-post-one");
            Assert.NotNull(post);

            var postId = post.PostId;

            var comment = new Comment
            {
                Content = "Comment content for post 1",
                Author = "Bob",
                CreatedAt = DateTime.Now
            };

            // Act
            await _commentService.AddCommentAsync(postId, comment);

            // Assert
            var updatedPost = await _context.Posts
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
            await SeedTestData();
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Slug == "test-post-two");
            Assert.NotNull(post);
            var postId = post.PostId;

            var comment = new Comment
            {
                Content = "Comment content for post 2",
                Author = "William",
                CreatedAt = DateTime.Now
            };

            await _commentService.AddCommentAsync(postId, comment);
            Assert.Contains(post.Comments, c => c.Content == comment.Content
            && c.Author == comment.Author);

            comment.Content = "Edited Comment content for post 2";

            // Act
            await _commentService.EditCommentAsync(comment);

            // Assert
            var editedComment = await _context.Comments.FirstOrDefaultAsync(c => c.Content == comment.Content);
            Assert.NotNull(editedComment);
            Assert.Equal(comment.Content, editedComment.Content);
        }

        [Fact]
        public async Task DeleteCommentAsync_Should_Remove_Comment_If_Exists()
        {
            // Arrange
            await SeedTestData();

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Slug == "test-post-two");
            Assert.NotNull(post);
            var postId = post.PostId;

            var comment = new Comment
            {
                Content = "Comment content for post 2",
                Author = "William",
                CreatedAt = DateTime.Now
            };

            await _commentService.AddCommentAsync(postId, comment);

            var initialCommentCount = await _context.Comments.CountAsync();

            // Act
            await _commentService.DeleteCommentAsync(comment.CommentId);

            // Assert
            var deletedComment = await _context.Comments.FindAsync(comment.CommentId);
            var finalCommentCount = await _context.Comments.CountAsync();
            Assert.Null(deletedComment);
            Assert.Equal(initialCommentCount - 1, finalCommentCount);
        }
    }
}
