using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models;
using System.Security.Claims;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    public class CommentControllerTests : IClassFixture<CommentFixture>
    {
        private readonly CommentFixture _fixture;
        private readonly HttpClient _client;
        private readonly IServiceProvider _services;

        public CommentControllerTests(CommentFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client;
            _services = fixture.Services;
        }

        [Fact]
        public async Task OnAddCommentAsync_ShouldAddCommentToDatabaseAndReturn200OkResult()
        {
            // Arrange
            var contClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testContId"),
                new Claim(ClaimTypes.Name, "cont"),
                new Claim(ClaimTypes.Role, "Contributor"),
                new Claim("Comment", "2"),
                new Claim("Comment", "3"),
                new Claim("Comment", "4")
            };
            var contIdentity = new ClaimsIdentity(contClaims, "DynamicScheme");
            var contPrincipal = new ClaimsPrincipal(contIdentity);

            _fixture.SetCurrentUser(contPrincipal);

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            var postId = 1;
            var newComment = new Comment
            {
                Content = "Lorem ipsum dolor sit amet.",
                Author = "Jane",
                PostId = 1,
                UserId = "testContId"
            };

            var content = HttpHelper.GetJsonHttpContent(newComment);

            // Act
            var response = await _client.PostAsync(string.Format(HttpHelper.Urls.AddComment, postId), content);

            // Assert
            response.EnsureSuccessStatusCode();

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var addedComment = await dbContext.Comments
                    .FirstOrDefaultAsync(c => c.Content == newComment.Content &&
                                              c.Author == newComment.Author &&
                                              c.PostId == newComment.PostId);

                Assert.NotNull(addedComment);
                Assert.Equal(newComment.Content, addedComment.Content);
                Assert.Equal(newComment.Author, addedComment.Author);
                Assert.Equal(newComment.PostId, addedComment.PostId);
            }
        }

        [Fact]
        public async Task OnUpdateCommentAsync_ShouldUpdateCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange
            var contClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Name, "cont"),
                new Claim(ClaimTypes.Role, "Contributor"),
                new Claim("Comment", "2"),
                new Claim("Comment", "3"),
                new Claim("Comment", "4")
            };
            var contIdentity = new ClaimsIdentity(contClaims, "DynamicScheme");
            var contPrincipal = new ClaimsPrincipal(contIdentity);

            _fixture.SetCurrentUser(contPrincipal);

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            var commentToBeEdited = new EditCommentModel
            {
                Content = "Updated comment content."
            };

            var content = HttpHelper.GetJsonHttpContent(commentToBeEdited);

            // Act
            var response = await _client.PutAsync(HttpHelper.Urls.UpdateComment, content);

            // Assert
            response.EnsureSuccessStatusCode();

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var editedComment = await dbContext.Comments
                    .FirstOrDefaultAsync(c => c.Content == commentToBeEdited.Content);

                Assert.NotNull(editedComment);
                Assert.Equal(commentToBeEdited.Content, editedComment.Content);
            }
        }

        [Fact]
        public async Task OnDeleteCommentAsync_ShouldRemoveCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange
            var contClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Name, "cont"),
                new Claim(ClaimTypes.Role, "Contributor"),
                new Claim("Comment", "2"),
                new Claim("Comment", "3"),
                new Claim("Comment", "4")
            };
            var contIdentity = new ClaimsIdentity(contClaims, "DynamicScheme");
            var contPrincipal = new ClaimsPrincipal(contIdentity);

            _fixture.SetCurrentUser(contPrincipal);

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            int initialCount;

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                initialCount = await dbContext.Comments.CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync(HttpHelper.Urls.DeleteComment);

            // Assert
            response.EnsureSuccessStatusCode();

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var removedComment = await dbContext.Comments
                    .FirstOrDefaultAsync(c => c.Id == 3);

                var commentCount = await dbContext.Comments.CountAsync();

                Assert.Null(removedComment);
                Assert.Equal(initialCount - 1, commentCount);
            }
        }

        private async Task SeedDatabaseAsync(IEnumerable<Post> posts, ICollection<Category> categories)
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                if (await dbContext.Database.EnsureCreatedAsync())
                {
                    await dbContext.Categories.AddRangeAsync(categories);
                    await dbContext.Posts.AddRangeAsync(posts);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
