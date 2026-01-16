using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
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
            SetupMockUser();
            var invalidPostId = 0;

            var comment = new Comment { Content = "Valid content" };
            var content = HttpHelper.GetJsonHttpContent(comment);

            var url = string.Format(Comments.GetById, invalidPostId);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
        }

        [Fact]
        public async Task OnAddComment_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange            
            SetupMockUser();
            var postId = 1;

            var invalidComment = new Comment { Content = "" };
            var content = HttpHelper.GetJsonHttpContent(invalidComment);

            var url = string.Format(Comments.GetById, postId);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Any());
        }

        [Fact]
        public async Task OnAddCommentAsync_ShouldAddCommentToDatabaseAndReturn200OkResult()
        {
            // Arrange            
            const string userId = "testContId";
            SetupMockUser(userId);
            const string ExpectedCommentOutput = "Test comment content";
            const string HackCode = "Test comment content<script>Hack code</script>";

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            var postId = posts.First().Id;

            var createDto = TestDataHelper.CreateCommentRequest(HackCode);
            var content = HttpHelper.GetJsonHttpContent(createDto);

            var url = string.Format(Comments.GetById, postId);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentCreatedDto>>();

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(CommentM.Success.CommentAddedSuccessfully, result.Message);
            Assert.NotNull(result.Data);

            var data = result.Data!;
            Assert.Equal(ExpectedCommentOutput, data.Content);

            Assert.Equal(userId, data.UserId);

            Assert.True(data.Id > 0);
            Assert.NotEqual(default, data.CreatedAt);
            Assert.NotNull(data.Author);
        }

        [Fact]
        public async Task OnUpdateComment_ShouldReturnBadRequest_WhenCommentIdIsInvalid()
        {
            // Arrange
            SetupMockUser();
            var invalidCommentId = 0;

            var editModel = new CommentUpdateDto { Content = "Some valid content" };
            var content = HttpHelper.GetJsonHttpContent(editModel);

            var url = string.Format(Comments.GetById, invalidCommentId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
        }

        [Fact]
        public async Task OnUpdateComment_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            SetupMockUser();
            var commentId = 2;

            var invalidModel = new CommentUpdateDto { Content = "" };
            var content = HttpHelper.GetJsonHttpContent(invalidModel);

            var url = string.Format(Comments.GetById, commentId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Any());
        }

        [Fact]
        public async Task OnUpdateCommentAsync_ShouldUpdateCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange
            const string userId = "testContId";
            SetupMockUser(userId);
            int postId = 1;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            var commentToBeEdited = new CommentUpdateDto { Content = "Updated comment content." };

            var content = HttpHelper.GetJsonHttpContent(commentToBeEdited);
            var url = string.Format(Comments.GetById, postId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentUpdatedDto>>();

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(CommentM.Success.CommentUpdatedSuccessfully, result.Message);
            Assert.NotNull(result.Data);

            var data = result.Data!;
            Assert.Equal(commentToBeEdited.Content, data.Content);

            Assert.Equal(userId, data.UserId);

            Assert.True(data.Id > 0);
            Assert.NotEqual(default, data.CreatedAt);
            Assert.NotNull(data.Author);
        }

        [Fact]
        public async Task OnDeleteComment_ShouldReturnBadRequest_WhenCommentIdIsInvalid()
        {
            // Arrange           
            SetupMockUser();
            var invalidCommentId = 0;

            var url = string.Format(Comments.GetById, invalidCommentId);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
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

            var url = string.Format(Comments.GetById, commentIdToDelete);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(CommentM.Success.CommentDeletedSuccessfully, result.Message);
        }

        private async Task SeedDatabaseAsync(IEnumerable<Post> posts, ICollection<Category> categories)
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();

                var testUser = new IdentityUser
                {
                    Id = "testContId",
                    UserName = "testCont",
                    Email = "test@test.com",
                    NormalizedUserName = "TESTCONT"
                };

                await dbContext.Users.AddAsync(testUser);
                await dbContext.SaveChangesAsync();

                await dbContext.Categories.AddRangeAsync(categories);
                await dbContext.Posts.AddRangeAsync(posts);
                await dbContext.SaveChangesAsync();
            }
        }

        private void SetupMockUser(string userId = "testContId")
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
