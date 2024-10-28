using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models;
using PostApiService.Tests.E2ETests;
using System.Text;
using System.Text.Json;

namespace PostApiService.Tests.E2Tests
{
    public class PostsControllerE2ETests : IClassFixture<TestFactoryFixture>
    {
        private readonly TestFactoryFixture _fixture;
        private readonly HttpClient _client;
        public PostsControllerE2ETests(TestFactoryFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client;
        }

        [Fact]
        public async Task GetPosts_ReturnsOk_WithListOfPosts()
        {
            // Arrange       
            var post1 = CreateTestPost(
                 "Test Post 1",
                 "Post Content",
                 "Maks",
                 "src/image.jpg",
                 "Some Description",
                 "some-slug-post",
                 "Some meta title",
                 "Some meta description"
            );

            var post2 = CreateTestPost(
                "Test Post 2",
                "Post Content 2",
                "Maks 2",
                "src/image2.jpg",
                "Some Description 2",
                "some-slug-post 2",
                "Some meta title 2",
                "Some meta description 2"
            );

            await SeedPostAsync(post1);
            await SeedPostAsync(post2);

            // Act
            var response = await _client.GetAsync("/api/Posts");

            // Assert
            response.EnsureSuccessStatusCode(); // Verifies the status code is 2xx

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);

            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var postList = await context.Posts.ToListAsync();

            Assert.NotNull(postList);
            Assert.Equal(2, postList.Count);
            Assert.Contains(postList, p => p.Title == "Test Post 1");
            Assert.Contains(postList, p => p.Title == "Test Post 2");
        }

        [Fact]
        public async Task GetPostById_ShouldReturnOk_WithPostById()
        {
            // Arrange            
            var post1 = CreateTestPost(
                "Test Post 1",
                "Post Content",
                "Maks",
                "src/image.jpg",
                "Some Description",
                "some-slug-post",
                "Some meta title",
                "Some meta description"
            );

            var post2 = CreateTestPost(
                "Test Post 2",
                "Post Content 2",
                "Maks 2",
                "src/image2.jpg",
                "Some Description 2",
                "some-slug-post 2",
                "Some meta title 2",
                "Some meta description 2"
            );

            await SeedPostAsync(post1);
            await SeedPostAsync(post2);

            // Act
            var response = await _client.GetAsync($"/api/Posts/{post2.PostId}");

            // Assert
            response.EnsureSuccessStatusCode();

            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var postList = await context.Posts.ToListAsync();

            Assert.NotNull(postList);
            Assert.Contains(postList, p => p.Title == post2.Title);
        }

        [Fact]
        public async Task AddPost_ShouldAddPost_ReturnOk_WhenPostIsValid()
        {
            // Arrange        
            var postToBeAdded = CreateTestPost(
                "Test title",
                "Test Content",
                "Test Author",
                "src/image.jpg",
                "Test Description",
                "Test Slug",
                "Test Meta Title",
                "Test Meta Description"
            );

            var postJson = JsonSerializer.Serialize(postToBeAdded);
            var content = new StringContent(postJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Posts", content);

            // Assert
            response.EnsureSuccessStatusCode();

            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var addedPost = await context.Posts.FirstOrDefaultAsync(p => p.Title == postToBeAdded.Title);
            Assert.NotNull(addedPost);
            Assert.Equal(postToBeAdded.Content, addedPost.Content);
            Assert.Equal(postToBeAdded.Author, addedPost.Author);
        }

        [Fact]
        public async Task EditPost_ShouldEditPostById_ReturnOk_IsPostValid()
        {
            // Arrange           
            var post = CreateTestPost(
                "Tes title",
                "Test Content",
                "Test Author",
                "src/image.jpg",
                "Test Description",
                "Test Slug",
                "Test Meta Title",
                "Test Meta Description"
            );

            await SeedPostAsync(post);

            post.Title = "Changed";
            post.Slug = "changed-slug";

            var updatedPostJson = JsonSerializer.Serialize(post);
            var content = new StringContent(updatedPostJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"api/posts/{post.PostId}", content);

            // Assert
            response.EnsureSuccessStatusCode();

            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var editedPost = await context.Posts.FirstOrDefaultAsync(p => p.Title == "Changed");
            Assert.NotNull(editedPost);
            Assert.Equal("Changed", editedPost.Title);
            Assert.Equal("changed-slug", editedPost.Slug);
        }

        [Fact]
        public async Task DeletePost_ShouldRemovePost_IfExists()
        {
            // Arrange            
            var postToBeDeleted = CreateTestPost(
                "Removed title",
                "Removed Content",
                "Removed Author",
                "src/remove.jpg",
                "Removed Description",
                "Removed Slug",
                "Removed Meta Title",
                "Removed Meta Description"
            );

            await SeedPostAsync(postToBeDeleted);

            // Act
            var response = await _client.DeleteAsync($"api/posts/{postToBeDeleted.PostId}");

            // Assert
            response.EnsureSuccessStatusCode();

            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deletedPost = await context.Posts.FindAsync(postToBeDeleted.PostId);

            Assert.Null(deletedPost);
        }

        private Post CreateTestPost(
            string title,
            string content,
            string author,
            string imageUrl,
            string description,
            string slug,
            string metaTitle,
            string metaDescription)
        {
            return new Post
            {
                Title = title,
                Content = content,
                Author = author,
                ImageUrl = imageUrl,
                CreateAt = DateTime.UtcNow,
                Description = $"Description for {title}",
                Slug = slug,
                MetaTitle = $"Meta title for {title}",
                MetaDescription = $"Meta description for {title}"
            };
        }

        private async Task SeedPostAsync(Post post)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Posts.Add(post);
            await context.SaveChangesAsync();
        }
    }
}
