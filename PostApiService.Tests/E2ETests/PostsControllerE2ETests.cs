using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models;
using PostApiService.Services;
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
            using var scope = _fixture.Factory!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var postService = new PostService(context);

            //context.Posts.RemoveRange(context.Posts);
            //await context.SaveChangesAsync();

            // Arrange       
            var post = CreateTestPost(
                 "Test Post 1",
                 "Post Content",
                 "Maks",
                 "src/image.jpg",
                 "Some Description",
                 "some-slug-post",
                 "Some meta title",
                 "Some meta description"
            );

            await postService.AddPostAsync(post);

            // Act
            var response = await _client.GetAsync("/api/posts");

            // Assert
            response.EnsureSuccessStatusCode(); // Verifies the status code is 2xx

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);

            var postList = await context.Posts.ToListAsync();
            Assert.NotNull(postList);           
            Assert.Contains(postList, p => p.Title == "Test Post 1");
        }

        [Fact]
        public async Task GetPostById_ShouldReturnOk_WithPostById()
        {
            using var scope = _fixture.Factory!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var postService = new PostService(context);

            //context.Posts.RemoveRange(context.Posts);
            //await context.SaveChangesAsync();

            // Arrange            
            var post = CreateTestPost(
                "Test Post 1",
                "Post Content",
                "Maks",
                "src/image.jpg",
                "Some Description",
                "some-slug-post",
                "Some meta title",
                "Some meta description"
            );

            await postService.AddPostAsync(post);

            // Act
            var response = await _client.GetAsync($"/api/posts/{post.PostId}");

            // Assert
            response.EnsureSuccessStatusCode();

            var foundPost = await context.Posts.FirstOrDefaultAsync(p => p.Slug == post.Slug);

            Assert.NotNull(foundPost);
            Assert.Equal(post.Title, foundPost.Title);
        }

        [Fact]
        public async Task AddPost_ShouldAddPost_ReturnOk_WhenPostIsValid()
        {
            using var scope = _fixture.Factory!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            //// Ensure the database is clean before each test
            //context.Posts.RemoveRange(context.Posts);
            //await context.SaveChangesAsync();

            //Arrange
            var newPost = CreateTestPost(
                "Test title",
                "Test Content",
                "Test Author",
                "src/image.jpg",
                "Test Description",
                "Test Slug",
                "Test Meta Title",
                "Test Meta Description"
            );

            var postJson = JsonSerializer.Serialize(newPost);
            var content = new StringContent(postJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/posts", content);

            // Assert
            response.EnsureSuccessStatusCode();

            var addedPost = await context.Posts.FirstOrDefaultAsync(p => p.Title == newPost.Title);
            Assert.Equal(newPost.Content, addedPost.Content);
            Assert.Equal(newPost.Author, addedPost.Author);
        }

        [Fact]
        public async Task EditPost_ShouldEditPostById_ReturnOk_IsPostValid()
        {
            // Arrange            
            using var scope = _fixture.Factory!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            //context.Posts.RemoveRange(context.Posts);
            //await context.SaveChangesAsync();

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

            await context.Posts.AddAsync(post);
            await context.SaveChangesAsync();

            post.Title = "Changed";
            post.Slug = "changed-slug";

            var updatedPostJson = JsonSerializer.Serialize(post);
            var content = new StringContent(updatedPostJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"api/posts/{post.PostId}", content);

            // Assert
            response.EnsureSuccessStatusCode();

            var editedPost = await context.Posts.FirstOrDefaultAsync(p => p.Title == "Changed");
            Assert.NotNull(editedPost);
            Assert.Equal("Changed", editedPost.Title);
            Assert.Equal("changed-slug", editedPost.Slug);
        }

        [Fact]
        public async Task DeletePost_ShouldRemovePost_IfExists()
        {
            // Arrange            
            using var scope = _fixture.Factory!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.Posts.RemoveRange(context.Posts);
            await context.SaveChangesAsync();

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

            await context.Posts.AddAsync(postToBeDeleted);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.DeleteAsync($"api/posts/{postToBeDeleted.PostId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var countBefore = await context.Posts.CountAsync();
            var deletedPost = await context.Posts.FindAsync(1);
            var countAfter = await context.Posts.CountAsync();
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
    }
}
