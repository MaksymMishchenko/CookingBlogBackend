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
        public async Task GetActivePostsAsync_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
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
        public async Task GetActivePostsAsync_ShouldReturnPagedPosts()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            const int PageNumber = 1;
            const int PageSize = 10;
            const int ExpectedTotalPosts = 7;
            const int ExpectedCommentCountPerPost = 12;

            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPosts, categories, commentCount: ExpectedCommentCountPerPost);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.Paginated, PageNumber, PageSize);

            // Act            
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<PostListDto>>>();

            response.EnsureSuccessStatusCode();

            // Assert
            Assert.NotNull(content);
            Assert.True(content.Success);

            Assert.NotNull(content.Data);
            Assert.Equal(ExpectedTotalPosts, content.TotalCount);

            var expectedPostsSorted = posts
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            Assert.All(content.Data, (postDto, index) =>
            {
                int expectedIndex = (PageNumber - 1) * PageSize + index;
                var expectedPost = expectedPostsSorted[expectedIndex];

                TestDataHelper.AssertPostListDtoMapping(expectedPost, postDto, ExpectedCommentCountPerPost);
            });
        }

        [Fact]
        public async Task GetActivePostsAsync_ShouldReturnEmptyList_WhenNoPostsAvailableYet()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            const int PageNumber = 1;
            const int PageSize = 3;
            const int ExpectedTotalPosts = 0;
            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments(count: ExpectedTotalPosts, categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.Paginated, PageNumber, PageSize);

            // Act
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<PostListDto>>>();

            // Assert            
            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.Empty(content.Data);
            Assert.Equal(ExpectedTotalPosts, content.TotalCount);
        }

        [Fact]
        public async Task GetActivePostsAsync_ShouldExcludeInactivePosts_WhenMixedContentExists()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments(count: 5, categories);

            posts[0].IsActive = false;
            posts[1].IsActive = false;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.Paginated, 1, 10);

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<PostListDto>>>();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(content);

            Assert.Equal(3, content.TotalCount);
            Assert.Equal(3, content.Data!.Count);

            var inactiveSlugs = posts.Where(p => !p.IsActive).Select(p => p.Slug);
            Assert.All(content.Data, dto => Assert.DoesNotContain(dto.Slug, inactiveSlugs));
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
            bool? filterStatus = null;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(count: TotalCount, categories);

            posts.First().IsActive = false;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.AdminPaginated, filterStatus, PageNumber, PageSize);           

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
        public async Task SearchActivePostsAsync_ShouldReturnBadRequest_WhenQueryIsTooShort()
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 3;
            const string EmptyQuery = " ";
            var url = string.Format(Posts.Search, EmptyQuery, PageNumber, PageSize);

            // Act            
            var response = await _client.GetAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            Assert.False(content!.Success);
            Assert.NotNull(content.Message);
        }

        [Fact]
        public async Task SearchActivePostsAsync_Pagination_ShouldReturnsSuccessAndCorrectData()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            const int PageNumber = 1;
            const int PageSize = 3;
            const int ExpectedTotalPosts = 3;
            const string Query = "Chili";

            var url = string.Format(Posts.Search, Query, PageNumber, PageSize);

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetSearchedPostWithoutIds(categories);

            var expectedPostsSorted = posts
                .Where(p => p.Title.Contains(Query) || p.Content.Contains(Query) || p.Description.Contains(Query))
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            // Act            
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchPostListDto>>>();

            // Assert
            Assert.NotNull(content);
            Assert.True(content.Success);

            Assert.NotNull(content.Data);
            Assert.Equal(PageSize, content.PageSize);

            Assert.Equal(ExpectedTotalPosts, content.TotalCount);
            Assert.Equal("Chili", content.SearchQuery);

            Assert.All(content.Data, (postDto, index) =>
            {
                int expectedIndex = (PageNumber - 1) * PageSize + index;
                var expectedPost = expectedPostsSorted[expectedIndex];

                TestDataHelper.AssertSearchActivePostsPagedAsync(expectedPost, postDto);
            });
        }

        [Fact]
        public async Task SearchActivePostsAsync_ShouldExcludeInactivePosts_EvenIfTheyMatchQuery()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            const string Query = "Chili";
            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments(count: 2, categories);

            var activePost = posts[0];
            activePost.Title = $"Amazing {Query} Recipe";
            activePost.IsActive = true;

            var inactivePost = posts[1];
            inactivePost.Title = $"Hidden {Query} Secret";
            inactivePost.IsActive = false;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.Search, Query, 1, 10);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchPostListDto>>>();

            Assert.NotNull(content);
            Assert.True(content.Success);

            Assert.Equal(1, content.TotalCount);
            Assert.Single(content.Data);

            Assert.Equal(activePost.Slug, content.Data[0].Slug);
            Assert.DoesNotContain(content.Data, p => p.Slug == inactivePost.Slug);
        }

        [Theory]
        [InlineData("Desserts")]
        [InlineData("dessert_slug")]
        [InlineData("dessert@!")]
        [InlineData(" ")]
        public async Task GetActivePostsByCategoryAsync_ShouldReturnBadRequest_WhenSlugIsInvalid(string invalidSlug)
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 10;

            var url = string.Format(Posts.GetByCategorySlug, invalidSlug, PageNumber, PageSize);

            // Act
            var response = await _client.GetAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(content);
            Assert.False(content.Success);

            Assert.True(content.Errors!.Count > 0);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(1, 0)]
        [InlineData(1, 51)]
        public async Task GetActivePostsByCategoryAsync_ShouldReturnBadRequest_WhenPaginationIsInvalid(int pageNumber, int pageSize)
        {
            // Arrange
            const string ValidSlug = "desserts";
            var url = string.Format(Posts.GetByCategorySlug, ValidSlug, pageNumber, pageSize);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(content);
            Assert.False(content.Success);
            Assert.True(content.Errors!.ContainsKey("PageNumber") || content.Errors.ContainsKey("PageSize"));
        }

        [Fact]
        public async Task GetActivePostsByCategoryAsync_ShouldReturnOk_WithFilteredPosts()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            const string TargetCategorySlug = "desserts";
            const int PostsInTarget = 3;
            const int PostsInOther = 2;
            const int PageNumber = 1;
            const int PageSize = 10;

            var categories = TestDataHelper.GetCulinaryCategories();
            var targetCategory = categories.First(c => c.Slug == TargetCategorySlug);
            var otherCategory = categories.First(c => c.Slug != TargetCategorySlug);

            var targetPosts = TestDataHelper.GetPostsWithComments(PostsInTarget, categories, forcedCategory: targetCategory);
            var otherPosts = TestDataHelper.GetPostsWithComments(PostsInOther, categories, forcedCategory: otherCategory);

            var allPosts = targetPosts.Concat(otherPosts).ToList();

            allPosts.ForEach(p => p.Slug = $"{p.Slug}-{Guid.NewGuid().ToString()[..4]}");

            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            var url = string.Format(Posts.GetByCategorySlug, TargetCategorySlug, PageNumber, PageSize);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<PostListDto>>>();

            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.NotNull(content.Data);

            Assert.Equal(PostsInTarget, content.TotalCount);
            Assert.Equal(PostsInTarget, content.Data.Count);

            Assert.All(content.Data, post =>
            {
                Assert.Equal(TargetCategorySlug, post.CategorySlug);
            });

            var expectedFirstId = targetPosts.OrderByDescending(p => p.CreatedAt).First().Id;
            Assert.Equal(expectedFirstId, content.Data.First().Id);
        }

        [Fact]
        public async Task GetActivePostsByCategoryAsync_ShouldExcludeInactivePosts_EvenInTargetCategory()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            const string TargetCategorySlug = "desserts";
            const int ActiveCount = 3;
            const int InactiveCount = 2;
            const int PageNumber = 1;
            const int PageSize = 10;

            var categories = TestDataHelper.GetCulinaryCategories();
            var targetCategory = categories.First(c => c.Slug == TargetCategorySlug);

            var posts = TestDataHelper.GetPostsWithComments(
                ActiveCount + InactiveCount,
                categories,
                forcedCategory: targetCategory);

            for (int i = 0; i < posts.Count; i++)
            {
                posts[i].IsActive = i < ActiveCount;
                posts[i].Slug = $"{posts[i].Slug}-{Guid.NewGuid().ToString()[..4]}";
            }

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var url = string.Format(Posts.GetByCategorySlug, TargetCategorySlug, PageNumber, PageSize);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<PostListDto>>>();

            Assert.NotNull(content);
            Assert.True(content.Success);

            Assert.Equal(ActiveCount, content.TotalCount);
            Assert.Equal(ActiveCount, content.Data!.Count);

            var inactiveSlugs = posts.Where(p => !p.IsActive).Select(p => p.Slug);
            Assert.All(content.Data, dto =>
            {
                Assert.DoesNotContain(dto.Slug, inactiveSlugs);
                Assert.Equal(TargetCategorySlug, dto.CategorySlug);
            });
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
            var response = await _client.PostAsJsonAsync(Posts.Base, invalidPostDto);

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
            var response = await _client.PostAsJsonAsync(Posts.Base, postToCreate);
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

