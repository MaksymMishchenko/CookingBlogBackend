using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class PostServiceIntegrationTests : IClassFixture<InMemoryDatabaseFixture>, IAsyncLifetime
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

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSpecificPost()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var postId = 1;

            // Act
            var post = await postService.GetPostByIdAsync(postId, includeComments: false);

            // Assert
            Assert.NotNull(post);
            Assert.Equal(postId, post.PostId);
            Assert.NotNull(post.Comments);
            Assert.Empty(post.Comments);
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddNewPostSuccessfully()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var newPost = TestDataHelper.GetPostWithComments(generateComments: false);
            var initialCount = await context.Posts.CountAsync();

            // Act
            await postService.AddPostAsync(newPost);

            // Assert
            var addedPost = await context.Posts
                .FirstOrDefaultAsync(p => p.Title == newPost.Title && p.Author == newPost.Author);
            Assert.NotNull(addedPost);
            Assert.Equal(newPost.Title, addedPost.Title);
            Assert.Equal(newPost.Author, addedPost.Author);
            Assert.Equal(newPost.Content, addedPost.Content);
            Assert.NotEqual(DateTime.MinValue, addedPost.CreateAt);
            Assert.True(addedPost.CreateAt <= DateTime.UtcNow.ToLocalTime());

            var postCount = await context.Posts.CountAsync();
            Assert.Equal(initialCount + 1, postCount);
            Assert.NotNull(addedPost.Comments);
            Assert.Empty(addedPost.Comments);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldUpdatedExistingPostSuccessfully()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var postId = 1;
            var existingPost = await context.Posts.FindAsync(postId);
            Assert.NotNull(existingPost);

            existingPost.Title = "Updated title";
            existingPost.Content = "Updated content";
            existingPost.MetaTitle = "Updated meta title";

            // Act
            await postService.UpdatePostAsync(existingPost);

            // Assert
            var updatedPost = await context.Posts.FindAsync(postId);
            Assert.NotNull(updatedPost);
            Assert.Equal(existingPost.Title, updatedPost.Title);
            Assert.Equal(existingPost.Content, updatedPost.Content);
            Assert.NotEqual(DateTime.MinValue, updatedPost.CreateAt);
            Assert.True(updatedPost.CreateAt <= DateTime.UtcNow.ToLocalTime());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldRemovePostSuccessfully()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var initialCount = await context.Posts.CountAsync();
            var postId = 1;

            // Act
            await postService.DeletePostAsync(postId);

            // Assert
            var removedPost = await context.Posts
                .AnyAsync(p => p.PostId == postId);
            Assert.False(removedPost);
        }

        public Task InitializeAsync() => _fixture.InitializeAsync();

        public Task DisposeAsync() => _fixture.DisposeAsync();
    }
}
