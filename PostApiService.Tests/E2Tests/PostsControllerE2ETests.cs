using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models;
using System.Text.Json;

namespace PostApiService.Tests.E2Tests
{
    public class PostsControllerE2ETests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        public PostsControllerE2ETests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });
            _client = _factory.CreateClient();
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

            var posts = JsonSerializer.Deserialize<List<Post>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(posts);
            Assert.Equal(2, posts.Count);
            Assert.Contains(posts, p => p.Title == "Test Post 1");
            Assert.Contains(posts, p => p.Title == "Test Post 2");
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
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);

            var post = JsonSerializer.Deserialize<Post>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(post);
            Assert.Equal(post2.Title, post.Title);
            Assert.Equal(post2.Slug, post.Slug);
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
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Posts.Add(post);
                await context.SaveChangesAsync();
            }
        }
    }
}
