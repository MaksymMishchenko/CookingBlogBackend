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

        [Fact]
        public async Task AddPostAsync_Should_Add_Post_To_Database()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);

            var post = new Post
            {
                Title = "New Post",
                Description = "Description",
                Content = "Content",
                Author = "Author",
                Slug = "new-post",
                ImageUrl = "image.jpg",
                MetaDescription = "Post meta description",
                MetaTitle = "Post meta title"
            };

            // Act
            await postService.AddPostAsync(post);

            // Assert
            var savedPost = await context.Posts.FirstOrDefaultAsync(p => p.Slug == post.Slug);
            Assert.NotNull(savedPost);
            Assert.Equal(post.Title, savedPost.Title);
        }

        [Fact]
        public async Task EditPostAsync_ShouldUpdatePost_IfExist()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);

            var post = CreateTestPost(
                "Origin Post",
                "Origin Content",
                "Origin Author",
                "Origin Description",
                "origin-image.jpg",
                "origin-post",
                "Origin Post meta title",
                "Origin Post meta description"
                );

            await SeedDataAsync(post);

            post.Title = "Updated post";

            // Act
            await postService.EditPostAsync(post);

            // Assert
            var updatedPost = await context.Posts.FirstOrDefaultAsync(p => p.Title == post.Title);
            Assert.NotNull(updatedPost);
            Assert.Equal(post.Title, updatedPost.Title);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldRemovePostById_IfExist()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var postService = new PostService(context);

            int postId = 1;

            var post = CreateTestPost(
                "Test Post",
                "Test Content",
                "Test Author",
                "Test Description",
                "test-image.jpg",
                "Test-post",
                "Test Post meta title",
                "Test Post meta description"
                );

            await SeedDataAsync(post);

            // Act
            await postService.DeletePostAsync(postId);

            // Assert
            var removedPost = await context.Posts.FindAsync(postId);
            Assert.Null(removedPost);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnListPosts()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var postService = new PostService(context);

            var post1 = CreateTestPost(
                "Test Post 1",
                "Test Content 1",
                "Test Author 1",
                "Test Description 1",
                "test-image1.jpg",
                "Test-post 1",
                "Test Post meta title 1",
                "Test Post meta description 1"
                );

            var post2 = CreateTestPost(
                "Test Post 2",
                "Test Content 2",
                "Test Author 2",
                "Test Description 2",
                "test-image 2.jpg",
                "Test-post 2",
                "Test Post meta title 2",
                "Test Post meta description 2"
                );

            await SeedDataAsync(post1);
            await SeedDataAsync(post2);

            // Act
            var posts = await postService.GetAllPostsAsync();

            // Assert
            Assert.NotNull(posts);
            Assert.Equal(2, posts.Count);
            Assert.Contains(posts, p => p.Title == post1.Title && p.Slug == post1.Slug);
            Assert.Contains(posts, p => p.Title == post2.Title && p.Slug == post2.Slug);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPostById_IfExists()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var postService = new PostService(context);

            int postId = 1;

            var post = CreateTestPost(
               "Test Post 1",
               "Test Content 1",
               "Test Author 1",
               "Test Description 1",
               "test-image1.jpg",
               "Test-post 1",
               "Test Post meta title 1",
               "Test Post meta description 1"
               );

            await SeedDataAsync(post);

            // Act
            var foundPost = await postService.GetPostByIdAsync(postId);

            // Assert
            Assert.NotNull(foundPost);
            Assert.Equal(post.Title, foundPost.Title);
        }

        private Post CreateTestPost(string title,
            string content,
            string author,
            string description,
            string imageUrl,
            string slug,
            string metaTitle,
            string metaDescription)
        {
            return new Post
            {
                Title = title,
                Content = content,
                Author = author,
                CreateAt = DateTime.Now,
                Description = description,
                ImageUrl = imageUrl,
                Slug = slug,
                MetaTitle = metaTitle,
                MetaDescription = metaDescription
            };
        }

        private async Task SeedDataAsync(Post post)
        {
            var context = _fixture.CreateContext();
            await context.Posts.AddAsync(post);
            await context.SaveChangesAsync();
        }
    }
}
