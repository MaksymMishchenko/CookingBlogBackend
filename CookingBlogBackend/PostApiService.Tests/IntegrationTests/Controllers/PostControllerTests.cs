using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests
{
    [Collection("SharedDatabase")]
    public class PostControllerTests
    {
        private readonly BaseTestFixture _fixture;
        private readonly HttpClient _client;
        private readonly IServiceProvider _services;

        public PostControllerTests(BaseTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
            _services = fixture.Services!;
        }

        [Fact]
        public async Task GetPostsAsync_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 50;
            var url = string.Format(Posts.Paginated, pageNumber, pageSize);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);

            var expectedMessage = string.Format(Global.Validation.PageSizeExceeded, 10);
            Assert.Equal(expectedMessage, result.Errors!["PageSize"][0]);
        }

        [Fact]
        public async Task GetPostsAsync_NormalMode_ShouldReturnActivePostsWithMapping()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(3, categories);
            posts[0].IsActive = false;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);
            var url = string.Format(Posts.Paginated, 1, 10);

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<PostListDto>>>();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(content);
            Assert.Equal(2, content.TotalCount);
            Assert.IsType<PostListDto>(content.Data!.First());
        }

        [Fact]
        public async Task GetPostsAsync_SearchMode_ShouldReturnSearchDtosWithSnippets()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            const string Term = "pizza";

            var posts = TestDataHelper.GetPostsWithComments(2, categories);
            posts[0].Title = $"Best {Term} recipe";
            posts[0].IsActive = true;
            posts[1].Title = "Just a salad";
            posts[1].IsActive = true;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);
            var url = $"{Posts.Base}?search={Term}&pageNumber=1&pageSize=10";

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchPostListDto>>>();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(content);
            Assert.Equal(1, content.TotalCount);
            Assert.Equal(Term, content.SearchQuery);

            var searchItem = content.Data!.First();
            Assert.NotNull(searchItem.SearchSnippet);
        }        

        [Theory]
        [InlineData("Invalid-Category", "valid-slug")]
        [InlineData("category!", "slug")]
        public async Task GetActivePostBySlugAsync_ShouldReturnBadRequest_WhenModelIsInvalid(string cat, string slug)
        {
            // Arrange                        
            var url = string.Format(Posts.GetBySlug, cat, slug);

            // Act
            var response = await _client.GetAsync(url);

            // Assert           
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);

            var hasCategoryError = result.Errors!.ContainsKey("Category") &&
                           result.Errors["Category"].Contains(Global.Validation.SlugFormat);

            var hasSlugError = result.Errors!.ContainsKey("Slug") &&
                               result.Errors["Slug"].Contains(Global.Validation.SlugFormat);

            Assert.True(hasCategoryError || hasSlugError, "Should contain slug format error message");
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            var nonExistentCategory = "ghost-category";
            var nonExistentSlug = "non-existent-post-slug";

            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.GetBySlug, nonExistentCategory, nonExistentSlug);

            // Act
            var response = await _client.GetAsync(url);

            // Assert           
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(PostM.Errors.PostNotFoundByPath, result.Message);
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ShouldReturnPost_WhenCategoryAndSlugAreValidAndExist()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var expectedPost = posts.First();
            var existingCategory = expectedPost.Category.Slug;
            var existingSlug = expectedPost.Slug;

            var url = string.Format(Posts.GetBySlug, existingCategory, existingSlug);

            // Act
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PostDetailsDto>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);

            Assert.Equal(existingSlug, result.Data!.Slug);
            Assert.Equal(existingCategory, result.Data.CategorySlug);
            Assert.Equal(expectedPost.Title, result.Data.Title);
        }

        [Fact]
        public async Task GetActivePostBySlug_ShouldReturnNotFound_WhenPostIsInactive()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            var category = categories.First();

            var posts = TestDataHelper.GetPostsWithComments(count: 1, categories, forcedCategory: category);
            var inactivePost = posts.First();

            inactivePost.IsActive = false;
            inactivePost.Slug = "hidden-gem-slug";

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.GetBySlug, category.Slug, inactivePost.Slug);

            // Act
            var response = await _client.GetAsync(url);

            // Assert           
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }        
    }
}

