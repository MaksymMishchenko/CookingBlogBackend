using Microsoft.Extensions.Logging;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private ApplicationDbContext _dbContext;
        private PostService _postService;
        private ILogger<PostService> _logger;

        public PostServiceTests(InMemoryDatabaseFixture fixture)
        {
            _dbContext = fixture.GetContext;
            _logger = new LoggerFactory().CreateLogger<PostService>();
            _postService = new PostService(_dbContext, _logger);
        }

        [Fact]
        public async Task AddPostAsync_ShoudReturnTrueWithPostId_IdPostAdded()
        {
            // Arrange            
            var newPost = GetPost();

            // Act
            var result = await _postService.AddPostAsync(newPost);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(newPost.PostId, result.PostId);
            Assert.Empty(newPost.Comments.ToList()); // comments list
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