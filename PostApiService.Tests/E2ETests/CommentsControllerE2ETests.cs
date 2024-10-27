using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace PostApiService.Tests.E2Tests
{
    public class CommentsControllerE2ETests : IClassFixture<WebApplicationFactory<Program>>
    {
        private HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public CommentsControllerE2ETests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;

        }
        private WebApplicationFactory<Program> CreateFactory()
        {
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (dbDescriptor != null)
                    {
                        services.Remove(dbDescriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });
        }

        [Fact]
        public async Task AddComment_ReturnsOk_WhenCommentIsAddedSuccessfully()
        {
            // Arrange
            var factory = CreateFactory();
            var client = factory.CreateClient();

            int postId = 1;
            var post = CreateTestPost(
                "Post title",
                "Post content",
                "Post author",
                "image.jpg",
                "Post desc",
                "post-slug",
                "Meta title",
                "Meta description"
            );

            await SeedPostAsync(post, factory);

            var newComment = new Comment
            {
                Content = "This is a test comment.",
                Author = "TestUser"
            };

            var jsonContent = JsonSerializer.Serialize(newComment);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync($"/api/comments/posts/{postId}", httpContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var addedComment = await context.Comments
                    .FirstOrDefaultAsync(c => c.Content == newComment.Content && c.PostId == postId);

                Assert.NotNull(addedComment);
                Assert.Equal(newComment.Content, addedComment.Content);
                Assert.Equal(newComment.Author, addedComment.Author);
                await context.Database.EnsureDeletedAsync();
            }
        }

        [Fact]
        public async Task EditComment_ShoulEditComment_IfExists()
        {
            // Arrange
            var factory = CreateFactory();
            var client = factory.CreateClient();

            int postId = 1;

            var post = CreateTestPost(
                "Post title",
                "Post content",
                "Post author",
                "image.jpg",
                "Post description",
                "post-slug",
                "Meta title",
                "Meta description"
            );

            await SeedPostAsync(post, factory);

            var initialComment = new Comment
            {
                Content = "This is a test comment.",
                Author = "Test Author"
            };

            var jsonContent = JsonSerializer.Serialize(initialComment);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var createResponse = await client.PostAsync($"/api/comments/posts/{postId}", httpContent);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            int commentId = 1;

            var updatedComment = new Comment
            {
                CommentId = commentId,
                Content = "Updated content"
            };

            var jsonUpdatedContent = JsonSerializer.Serialize(updatedComment);
            var httpUpdatedContent = new StringContent(jsonUpdatedContent, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PutAsync($"/api/comments/{commentId}", httpUpdatedContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var editedComment = await context.Comments
                    .FirstOrDefaultAsync(c => c.Content == "Updated content"
                    && c.Author == "Test Author");
                Assert.NotNull(editedComment);
                Assert.Equal(updatedComment.Content, editedComment.Content);
                await context.Database.EnsureDeletedAsync();
            }
        }

        private Post CreateTestPost(
            string title,
            string content,
            string author,
            string imageUrl,
            string description,
            string slug,
            string metaTitle,
            string metaDescription
            )
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

        private async Task SeedPostAsync(Post post, WebApplicationFactory<Program> factory)
        {
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Posts.Add(post);
                await context.SaveChangesAsync();
            }
        }
    }
}

