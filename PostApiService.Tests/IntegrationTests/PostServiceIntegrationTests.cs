using Microsoft.EntityFrameworkCore;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests
{
    public class PostServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public PostServiceIntegrationTests(IntegrationTestFixture fixture)
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
                Slug = "test-post-one"
            };

            var post2 = new Post
            {
                Title = "Test Post 2",
                Description = "This is a test post 2.",
                Content = "Content of the test post 2.",
                ImageUrl = "http://example.com/image2.jpg",
                MetaTitle = "Test Post Meta Title 2",
                MetaDescription = "Test Post Meta Description 2",
                Slug = "test-post-two"
            };

            await context.Posts.AddRangeAsync(post1, post2);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task AddPostAsync_Should_Add_Post_And_SaveChanges()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);
            await SeedTestData(context);

            const int postsCollectionCount = 3;

            var post3 = new Post
            {
                Title = "Test Post 3",
                Description = "This is a test post 3.",
                Content = "Content of the test post 3.",
                ImageUrl = "http://example.com/image3.jpg",
                MetaTitle = "Test Post Meta Title 3",
                MetaDescription = "Test Post Meta Description 3",
                Slug = "test-post-three"
            };

            // Act
            await postService.AddPostAsync(post3);

            // Assert
            var addedPost = await context.Posts.FindAsync(post3.PostId);
            var postCount = await context.Posts.CountAsync();
            Assert.NotNull(addedPost);
            Assert.Equal(postsCollectionCount, postCount);
            Assert.Equal(post3.Title, addedPost.Title);
            Assert.Equal(post3.Description, addedPost.Description);
            Assert.Equal(post3.Content, addedPost.Content);
            Assert.Equal(post3.ImageUrl, addedPost.ImageUrl);
            Assert.Equal(post3.MetaTitle, addedPost.MetaTitle);
            Assert.Equal(post3.MetaDescription, addedPost.MetaDescription);
            Assert.Equal(post3.Slug, addedPost.Slug);
        }

        [Fact]
        public async Task EditPostAsync_Should_Edit_Post_And_SaveChanges()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);
            await SeedTestData(context);

            int postToBeUpdatedId = 1;

            var postToBeUpdated = await postService.GetPostByIdAsync(postToBeUpdatedId);
            Assert.NotNull(postToBeUpdated);

            postToBeUpdated.Title = "Changed post title";
            postToBeUpdated.MetaTitle = "Changed Test Post Meta Title";

            // Act
            await postService.EditPostAsync(postToBeUpdated);

            // Assert
            var updatedPost = await context.Posts.FirstOrDefaultAsync(p => p.PostId == postToBeUpdatedId);
            Assert.NotNull(updatedPost);
            Assert.Equal(postToBeUpdatedId, updatedPost.PostId);
            Assert.Equal("Changed post title", updatedPost.Title);
            Assert.Equal("Changed Test Post Meta Title", updatedPost.MetaTitle);
        }

        [Fact]
        public async Task DeletePostAsync_Should_Remove_Post_If_Exists()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);
            await SeedTestData(context);
            int totalCount = await context.Posts.CountAsync();
            Assert.Equal(2, totalCount);

            var postToBeRemoved = await context.Posts.FirstOrDefaultAsync(p => p.Slug == "test-post-one");
            Assert.NotNull(postToBeRemoved);

            // Act
            await postService.DeletePostAsync(postToBeRemoved.PostId);

            // Assert
            var deletedPost = await context.Posts.FindAsync(postToBeRemoved.PostId);
            Assert.Null(deletedPost);

            int totalCountAfterRemove = await context.Posts.CountAsync();
            Assert.Equal(1, totalCountAfterRemove);
        }

        [Fact]
        public async Task GetAllPostsAsync_Should_Return_All_Posts()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);
            await SeedTestData(context);

            var post3 = new Post
            {
                Title = "Test Post 3",
                Description = "This is a test post 3.",
                Content = "Content of the test post 3.",
                ImageUrl = "http://example.com/image3.jpg",
                MetaTitle = "Test Post Meta Title 3",
                MetaDescription = "Test Post Meta Description 3",
                Slug = "test-post-three"
            };

            await postService.AddPostAsync(post3);

            // Act
            var posts = await postService.GetAllPostsAsync();

            // Assert
            Assert.NotNull(posts);
            var totalCount = await context.Posts.CountAsync();
            Assert.Equal(3, totalCount);
        }

        [Fact]
        public async Task GetPostByIdAsync_Should_Return_Post_If_Found()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);
            await SeedTestData(context);

            var addedPost = await context.Posts.FirstOrDefaultAsync(p => p.Content == "Content of the test post 2.");
            Assert.NotNull(addedPost);

            // Act
            var foundedPost = await postService.GetPostByIdAsync(addedPost.PostId);

            // Assert
            Assert.NotNull(foundedPost);
            Assert.Equal(addedPost.PostId, foundedPost.PostId);
        }
    }
}
