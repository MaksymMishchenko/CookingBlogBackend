using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models;
using PostApiService.Tests.E2ETests;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PostApiService.Tests.E2Tests
{
    public class CommentsControllerE2ETests : IClassFixture<TestFactoryFixture>
    {
        private readonly TestFactoryFixture _fixture;
        private readonly HttpClient _client;

        public CommentsControllerE2ETests(TestFactoryFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client;

        }
        //private WebApplicationFactory<Program> CreateFactory()
        //{
        //    return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        //    {
        //        builder.UseEnvironment("Test");
        //        builder.ConfigureServices(services =>
        //        {
        //            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

        //            if (dbDescriptor != null)
        //            {
        //                services.Remove(dbDescriptor);
        //            }

        //            services.AddDbContext<ApplicationDbContext>(options =>
        //            {
        //                options.UseInMemoryDatabase("TestDb");
        //            });
        //        });
        //    });
        //}

        [Fact]
        public async Task AddComment_ReturnsOk_WhenCommentIsAddedSuccessfully()
        {
            // Arrange           

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

            await SeedPostAsync(post);

            var newComment = new Comment
            {
                Content = "This is a test comment.",
                Author = "TestUser"
            };

            var jsonContent = JsonSerializer.Serialize(newComment);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/comments/posts/{postId}", httpContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var addedComment = await context.Comments
                .FirstOrDefaultAsync(c => c.Content == newComment.Content && c.PostId == postId);

            Assert.NotNull(addedComment);
            Assert.Equal(newComment.Content, addedComment.Content);
            Assert.Equal(newComment.Author, addedComment.Author);
        }

        [Fact]
        public async Task EditComment_ShoulEditComment_IfExists()
        {
            // Arrange           
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

            await SeedPostAsync(post);

            var initialComment = new Comment
            {
                Content = "This is a test comment.",
                Author = "Test Author"
            };

            var jsonContent = JsonSerializer.Serialize(initialComment);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var createResponse = await _client.PostAsync($"/api/comments/posts/{postId}", httpContent);
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
            var response = await _client.PutAsync($"/api/comments/{commentId}", httpUpdatedContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var editedComment = await context.Comments.FindAsync(commentId);

            Assert.NotNull(editedComment);
            Assert.Equal(updatedComment.Content, editedComment.Content);
        }

        [Fact]
        public async Task DeleteComment_ShoulRemoveCommentById()
        {
            // Arrange                        
            var postId = 1;

            var initialPost = CreateTestPost(
               "Post title",
               "Post content",
               "Post author",
               "postimg.jpg",
               "Post description",
               "post-slug",
               "Meta title",
               "Meta description"
                );

            await SeedPostAsync(initialPost);

            var initialComment = new Comment
            {
                Content = "Initial comment",
                Author = "Initial author",
                PostId = postId
            };

            var commentResponse = await _client.PostAsJsonAsync($"/api/comments/posts/{postId}", initialComment);
            Assert.Equal(HttpStatusCode.OK, commentResponse.StatusCode);

            // Act
            var response = await _client.DeleteAsync($"/api/comments/{initialComment.CommentId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var removedPost = await context.Comments.FindAsync(initialComment.CommentId);
            Assert.Null(removedPost);
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

        private async Task SeedPostAsync(Post post)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Posts.Add(post);
            await context.SaveChangesAsync();
        }
    }
}

