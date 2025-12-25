using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;
using System.Net.Http.Json;
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
            _client = fixture.Client!;
            _services = fixture.Services!;
        }

        [Fact]
        public async Task OnAddComment_ShouldReturnBadRequest_WhenPostIdIsInvalid()
        {
            // Arrange
            SetupMockUser("testContId");
            var invalidPostId = 0;

            var comment = new Comment { Content = "Valid content", Author = "Author" };
            var content = HttpHelper.GetJsonHttpContent(comment);

            var url = string.Format(HttpHelper.Urls.AddComment, invalidPostId);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(PostErrorMessages.InvalidPostIdParameter, result.Message);
        }

        [Fact]
        public async Task OnAddComment_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            SetupMockUser("testContId");
            var postId = 1;

            var invalidComment = new Comment { Content = "", Author = "" };
            var content = HttpHelper.GetJsonHttpContent(invalidComment);

            var url = string.Format(HttpHelper.Urls.AddComment, postId);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(ResponseErrorMessages.ValidationFailed, result.Message);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Any());
        }

        [Fact]
        public async Task OnAddCommentAsync_ShouldAddCommentToDatabaseAndReturn200OkResult()
        {
            // Arrange
            SetupMockUser("testContId");

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
        public async Task OnUpdateComment_ShouldReturnBadRequest_WhenCommentIdIsInvalid()
        {
            // Arrange
            SetupMockUser();
            var invalidCommentId = 0;

            var editModel = new EditCommentModel { Content = "Some valid content" };
            var content = HttpHelper.GetJsonHttpContent(editModel);

            var url = string.Format(HttpHelper.Urls.UpdateComment, invalidCommentId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(CommentErrorMessages.InvalidCommentIdParameter, result.Message);
        }

        [Fact]
        public async Task OnUpdateComment_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            SetupMockUser();
            var commentId = 2;

            var invalidModel = new EditCommentModel { Content = "" };
            var content = HttpHelper.GetJsonHttpContent(invalidModel);

            var url = string.Format(HttpHelper.Urls.UpdateComment, commentId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(ResponseErrorMessages.ValidationFailed, result.Message);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Any());
        }

        [Fact]
        public async Task OnUpdateCommentAsync_ShouldUpdateCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange
            SetupMockUser();
            int postId = 1;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            var commentToBeEdited = new EditCommentModel
            {
                Content = "Updated comment content."
            };

            var content = HttpHelper.GetJsonHttpContent(commentToBeEdited);
            var url = string.Format(HttpHelper.Urls.UpdateComment, postId);

            // Act
            var response = await _client.PutAsync(url, content);

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
        public async Task OnDeleteComment_ShouldReturnBadRequest_WhenCommentIdIsInvalid()
        {
            // Arrange           
            SetupMockUser();
            var invalidCommentId = 0;

            var url = string.Format(HttpHelper.Urls.DeleteComment, invalidCommentId);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(CommentErrorMessages.InvalidCommentIdParameter, result.Message);
        }

        [Fact]
        public async Task OnDeleteCommentAsync_ShouldRemoveCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange            
            SetupMockUser();
            int commentIdToDelete = 3;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            int initialCount;

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                initialCount = await dbContext.Comments.CountAsync();
            }

            var url = string.Format(HttpHelper.Urls.DeleteComment, commentIdToDelete);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var removedComment = await dbContext.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentIdToDelete);

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

        private void SetupMockUser(string userId = "testUserId")
        {
            var contClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "cont"),
                new Claim(ClaimTypes.Role, "Contributor"),
                new Claim("Comment", "2"),
                new Claim("Comment", "3"),
                new Claim("Comment", "4")
            };

            var contIdentity = new ClaimsIdentity(contClaims, "DynamicScheme");
            var contPrincipal = new ClaimsPrincipal(contIdentity);

            _fixture.SetCurrentUser(contPrincipal);
        }
    }
}
