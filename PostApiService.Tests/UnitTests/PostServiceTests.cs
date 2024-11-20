using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private InMemoryDatabaseFixture _fixture;

        public PostServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnTrueWithPostId_IfPostAdded()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var newPost = GetPost();

            // Act
            var result = await postService.AddPostAsync(newPost);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(newPost.PostId, result.PostId);
            Assert.Empty(newPost.Comments.ToList()); // comments list
        }

        [Fact]
        public async Task AddPostAsync_ShouldThrowArgumentNullException_IfPostIsNull()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => postService.AddPostAsync(null));
            Assert.Equal("Post cannot be null. (Parameter 'post')", exception.Message);
        }

        [Fact]
        public async Task EditPostAsync_ShouldReturnTrueWithPostId_IfPostEdited()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var newPost = GetPost();
            context.Posts.Add(newPost);
            await context.SaveChangesAsync();

            newPost.Title = "Edited title";
            newPost.Content = "Edited content";

            // Act
            var result = await postService.EditPostAsync(newPost);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.PostId);
            var editedPost = await context.Posts.FirstOrDefaultAsync(p => p.PostId == newPost.PostId);
            Assert.Equal(newPost.Title, editedPost.Title);
            Assert.Equal(newPost.Content, editedPost.Content);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldReturnFalseWithZero_IfPostDoesNotEdit()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var nonExistentPost = new Post
            {
                PostId = 999,
                Title = "Non existent title",
                Content = "Non existent content",
                Description = "Non existent description",
                Slug = "non-existent-slug",
                ImageUrl = "non_existent_image",
                MetaTitle = "Non existent title",
                MetaDescription = "Non existent description"
            };

            // Act
            var result = await postService.EditPostAsync(nonExistentPost);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(0, result.PostId);
        }

        [Fact]
        public async Task EditPostAsync_ShouldThrowArgumentNullException_IfPostIsNull()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => postService.EditPostAsync(null));
            Assert.Equal("Post cannot be null. (Parameter 'post')", exception.Message);
        }

        [Fact]
        public async Task EditPostAsync_ShouldReturnFalseAndZero_WhenDbUpdateExceptionOccurs()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var service = new PostService(context, logger);

            var post = GetPost();
            context.Add(post);
            await context.SaveChangesAsync();
            context.Database.EnsureDeleted();

            // Act
            var result = await service.EditPostAsync(post);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(0, result.PostId);
        }

        [Fact]
        public async Task EditPostAsync_ShouldThrowException_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var service = new PostService(context, logger);

            var post = GetPost();
            context.Dispose(); 

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => service.EditPostAsync(post));
        }


        private Post GetPost()
        {
            return new Post
            {
                Title = "Test title",
                Content = "Test content",
                Description = "Test desc",
                Author = "Test author",
                Slug = "test-slug",
                ImageUrl = "testIamge.jpg",
                MetaTitle = "Test meta title",
                MetaDescription = "Test meta description"
            };
        }
    }
}