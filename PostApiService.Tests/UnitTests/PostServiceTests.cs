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