using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NUnit.Framework;
using PostApiService;
using PostApiService.Interfaces;
using PostApiService.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostApiService.Tests
{
    public class PostsControllerE2ETests
    {
        private HttpClient _client;
        private WebApplicationFactory<Program> _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Remove the real database context
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Add in-memory database for testing
                        services.AddDbContext<ApplicationDbContext>(options =>
                        {
                            options.UseInMemoryDatabase("TestDb");
                        });

                        // Optionally, you can mock IPostService here, or let it use the real service
                        // services.AddScoped<IPostService, MockPostService>();
                    });
                });

            _client = _factory.CreateClient();
        }

        [Test]
        public async Task GetAllPosts_ReturnsOk_WithEmptyList()
        {
            // Act
            var response = await _client.GetAsync("/api/posts");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("[]", content);  // Assuming the DB is empty
        }

        //[Test]
        //public async Task AddPost_ReturnsOk()
        //{
        //    // Arrange
        //    var post = new Post
        //    {
        //        Title = "New Post",
        //        Content = "This is a new post"
        //    };

        //    var jsonContent = new StringContent(JsonSerializer.Serialize(post), Encoding.UTF8, "application/json");

        //    // Act
        //    var response = await _client.PostAsync("/api/posts", jsonContent);

        //    // Assert
        //    response.EnsureSuccessStatusCode();
        //}

        //[Test]
        //public async Task GetPostById_ReturnsOk_WithPost()
        //{
        //    // Arrange: add a post first
        //    var post = new Post
        //    {
        //        Title = "Sample Post",
        //        Content = "This is a sample post"
        //    };

        //    var jsonContent = new StringContent(JsonSerializer.Serialize(post), Encoding.UTF8, "application/json");
        //    await _client.PostAsync("/api/posts", jsonContent);

        //    // Act: retrieve the post
        //    var response = await _client.GetAsync("/api/posts/1");

        //    // Assert
        //    response.EnsureSuccessStatusCode();
        //    var content = await response.Content.ReadAsStringAsync();
        //    Assert.IsTrue(content.Contains("Sample Post"));
        //}

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
