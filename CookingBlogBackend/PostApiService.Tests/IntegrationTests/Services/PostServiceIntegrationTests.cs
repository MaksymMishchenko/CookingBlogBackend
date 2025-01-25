using Microsoft.Extensions.Logging;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class PostServiceIntegrationTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;

        public PostServiceIntegrationTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        private PostService CreatePostService()
        {
            var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            return new PostService(context, logger);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnListOfPosts()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            // Act
            var listOfPosts = await postService.GetAllPostsAsync(1, 10, 1, 10, includeComments: false);

            // Assert
            Assert.NotEmpty(listOfPosts);
            Assert.Single(listOfPosts);
            Assert.Equal(1, listOfPosts[0].PostId);
            Assert.All(listOfPosts, post => Assert.Empty(post.Comments));
        }
    }
}
