using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    [Collection("SharedDatabase")]
    public class AdminPostsControllerTests
    {
        private readonly BaseTestFixture _fixture;
        private readonly HttpClient _client;
        private readonly IServiceProvider _services;

        public AdminPostsControllerTests(BaseTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
            _services = fixture.Services!;
        }

        [Fact]
        public async Task GetAdminPostsAsync_ShouldReturnAllPostsForAuthenticatedAdmin()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            _fixture.LoginAsAdmin();

            const int ActiveCount = 2;
            const int InactiveCount = 1;
            const int TotalCount = ActiveCount + InactiveCount;
            const int PageNumber = 1;
            const int PageSize = 10;
            bool? searchStatus = null;
            bool? categorySlugStatus = null;
            bool? onlyActiveStatus = null;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(count: TotalCount, categories);

            posts.First().IsActive = false;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.AdminPaginated, searchStatus, categorySlugStatus, onlyActiveStatus, PageNumber, PageSize);

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminPostListDto>>>();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(content);
            Assert.Equal(TotalCount, content.TotalCount);

            Assert.Contains(content.Data!, dto => dto.IsActive == false);
        }

        [Fact]
        public async Task GetAdminPostsAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            bool? searchStatus = null;
            bool? categorySlugStatus = null;
            bool? onlyActiveStatus = null;

            await _fixture.ResetDatabaseAsync();
            _fixture.LoginAsContributor();

            var url = string.Format(Posts.AdminPaginated, searchStatus, categorySlugStatus, onlyActiveStatus, 1, 10);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetPostById_ShouldReturnBadRequest_WhenPostIdIsInvalid()
        {
            // Arrange
            _fixture.LoginAsAdmin();

            var invalidPostId = 0;
            var url = string.Format(Posts.GetById, invalidPostId);

            // Act
            var response = await _client.GetAsync(url);

            // Assert           
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);

            Assert.Equal(Global.Validation.ValidationFailed, result.Message);

            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("id"));
            Assert.Equal(Global.Validation.InvalidId, result.Errors["id"][0]);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturn200ОК_WithExpectedPost()
        {
            // Arrange            
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            _fixture.LoginAsAdmin();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var title = posts.First().Title;

            var expectedPost = posts.First(p => p.Title == title);
            var realId = expectedPost.Id;

            // Act
            var response = await _client.GetAsync
                (string.Format(Posts.GetById, realId));
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PostAdminDetailsDto>>();
            response.EnsureSuccessStatusCode();

            // Assert            
            Assert.True(content!.Success);
            var data = content.Data!;

            Assert.Equal(realId, content.Data.Id);
            Assert.Equal(expectedPost!.Title, data.Title);
            Assert.Equal(expectedPost.Description, data.Description);
            Assert.Equal(expectedPost.MetaTitle, data.MetaTitle);
            Assert.Equal(expectedPost.Author, data.Author);
            Assert.Equal(expectedPost.Category.Id, data.CategoryId);
        }

        [Fact]
        public async Task GetPostById_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            _fixture.LoginAsAdmin();

            var nonExistentId = 9999;
            var url = string.Format(Posts.GetById, nonExistentId);

            // Act
            var response = await _client.GetAsync(url);

            // Assert           
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);
            Assert.Equal(string.Format(PostM.Errors.PostNotFound, nonExistentId), result.Message);
        }

        [Fact]
        public async Task AddPost_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            _fixture.LoginAsAdmin();

            var invalidPostDto = new PostCreateDto
            {
                Title = "",
                Content = "Valid content",
                Author = "Valid author"
            };

            // Act
            var response = await _client.PostAsJsonAsync(Posts.AdminBase, invalidPostDto);

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
        public async Task AddPostAsync_ShouldAddPost_Return201CreatedAtAction()
        {
            // Arrange          
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            _fixture.LoginAsAdmin();

            const string ExpectedSafeContent = "Test content";
            const string HackContent = "Test content<script>Hack code</script>";

            var categories = TestDataHelper.GetCulinaryCategories();

            await _fixture.Services!.SeedCategoriesAsync(categories);

            var postToCreate = TestDataHelper.GetCreatePostDto(HackContent, categories);

            // Act
            var response = await _client.PostAsJsonAsync(Posts.AdminBase, postToCreate);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PostAdminDetailsDto>>();

            var data = result!.Data;
            Assert.Equal(ExpectedSafeContent, data.Content);
            Assert.DoesNotContain("<script>", data.Content);

            var locationHeader = response.Headers.Location?.ToString();
            Assert.NotNull(locationHeader);

            Assert.True(result.Success);
            Assert.Equal(PostM.Success.PostAddedSuccessfully, result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(postToCreate.Title, result.Data.Title);

            var postId = result!.Data.Id;
            var postInDb = await _fixture.ExecuteInScopeAsync(db =>
                db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId));

            Assert.NotNull(postInDb);
            Assert.Equal(postToCreate.Title, postInDb!.Title);
            Assert.Equal(ExpectedSafeContent, postInDb.Content);
            Assert.True(postInDb.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task UpdatePost_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            _fixture.LoginAsAdmin();

            var postId = 1;
            var invalidPost = new PostUpdateDto
            {
                Title = "Valid Title",
                Content = "",
                Author = "Author"
            };

            var url = string.Format(Posts.GetById, postId);

            // Act
            var response = await _client.PutAsJsonAsync(url, invalidPost);

            // Assert           
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);

            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("Content"));
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturn200Ok_IfPostIsUpdatedSuccessfully()
        {
            // Arrange            
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            _fixture.LoginAsAdmin();

            const string ExpectedSafeContent = "Updated test content";
            const string HackContent = "Updated test content<script>Hack code</script>";
            const string NewTitle = "Absolutely New Title";

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var postToUpdate = posts.Last();
            var realId = postToUpdate.Id;

            var updateDto = TestDataHelper.GetPostUpdateDto(
                title: NewTitle,
                content: HackContent,
                categoryId: postToUpdate.CategoryId
            );

            var url = string.Format(Posts.GetById, realId);

            // Act                
            var response = await _client.PutAsJsonAsync(url, updateDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PostAdminDetailsDto>>();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(realId, result.Data.Id);
            Assert.Equal(ExpectedSafeContent, result.Data.Content);
            Assert.DoesNotContain("<script>", result.Data.Content);

            Assert.NotNull(result.Data.UpdatedAt);
            Assert.True(result.Data.UpdatedAt > DateTime.UtcNow.AddSeconds(-10));

            var updatedInDb = await _fixture.ExecuteInScopeAsync(db =>
                db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Title == NewTitle));

            Assert.NotNull(updatedInDb);
            Assert.Equal(NewTitle, updatedInDb.Title);
            Assert.Equal(ExpectedSafeContent, updatedInDb.Content);

            Assert.NotNull(updatedInDb.UpdatedAt);
            Assert.Equal(result.Data.UpdatedAt, updatedInDb.UpdatedAt);
        }

        [Fact]
        public async Task DeletePost_ShouldReturnBadRequest_WhenPostIdIsInvalid()
        {
            // Arrange
            _fixture.LoginAsAdmin();

            var invalidPostId = -1;
            var url = string.Format(Posts.GetById, invalidPostId);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);

            Assert.Equal(Global.Validation.ValidationFailed, result.Message);

            Assert.NotNull(result.Errors);
            Assert.Contains("postId", result.Errors.Keys);
            Assert.Equal(Global.Validation.InvalidId, result.Errors["postId"][0]);
        }

        [Fact]
        public async Task DeletePost_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            _fixture.LoginAsAdmin();

            var postId = 99999;
            var url = string.Format(Posts.GetById, postId);
            string errorMessage = PostM.Errors.PostNotFound;
            string errorCode = PostM.Errors.PostNotFoundCode;

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);

            Assert.Equal(errorMessage, result.Message);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn200Ok_IfPostDeletedSuccessfully()
        {
            // Arrange            
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            _fixture.LoginAsAdmin();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var postId = 4;
            var url = string.Format(Posts.GetById, postId);
            var successMessage = PostM.Success.PostDeletedSuccessfully;

            // Act
            var request = await _client.DeleteAsync(url);
            request.EnsureSuccessStatusCode();

            var response = await request.Content.ReadFromJsonAsync<ApiResponse<bool>>();

            // Assert           
            Assert.True(response!.Success);
            Assert.Equal(successMessage, response.Message);

            await _fixture.AssertEntityDeletedAsync<Post>(c => c.Id == postId);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldRemovePostAndAssociatedComments_Cascade()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            _fixture.LoginAsAdmin();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(count: 1, categories, commentCount: 3);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();
            var postId = targetPost.Id;
            var url = string.Format(Posts.GetById, postId);

            // Act
            var response = await _client.DeleteAsync(url);
            response.EnsureSuccessStatusCode();

            // Assert
            await _fixture.AssertEntityDeletedAsync<Post>(c => c.Id == postId);
            await _fixture.AssertEntityDeletedAsync<Comment>(c => c.PostId == postId);
        }
    }
}
