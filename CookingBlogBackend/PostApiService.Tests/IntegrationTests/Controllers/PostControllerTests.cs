using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PostApiService.Tests.IntegrationTests
{
    public class PostControllerTests : IClassFixture<PostFixture>
    {
        private readonly PostFixture _fixture;
        private readonly HttpClient _client;
        private readonly IServiceProvider _services;

        public PostControllerTests(PostFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
            _services = fixture.Services!;
        }

        [Fact]
        public async Task GetPosts_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
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
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnPagedPosts()
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 10;
            const int ExpectedTotalPosts = 7;
            const int ExpectedCommentCountPerPost = 12;

            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPosts, categories, commentCount: ExpectedCommentCountPerPost);
            await SeedDatabaseAsync(posts, categories);

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
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnEmptyList_WhenNoPostsAvailableYet()
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 3;
            const int ExpectedTotalPosts = 0;
            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments(count: ExpectedTotalPosts, categories);
            await SeedDatabaseAsync(posts, categories);

            var url = string.Format(Posts.Paginated, PageNumber, PageSize);

            // Act
            var response = await _client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            // Assert            
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<PostListDto>>>();

            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.Empty(content.Data);
            Assert.Equal(ExpectedTotalPosts, content.TotalCount);
        }

        [Fact]
        public async Task Search_ShouldReturnBadRequest_WhenQueryIsTooShort()
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
            Assert.False(content.Success);
            Assert.NotNull(content.Message);
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_Pagination_ShouldReturnsSuccessAndCorrectData()
        {
            // Arrange
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

            await SeedDatabaseAsync(posts, categories);

            // Act            
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchPostListDto>>>();

            response.EnsureSuccessStatusCode();

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

                TestDataHelper.AssertSearchPostsWithTotalCountAsync(expectedPost, postDto);
            });
        }

        [Fact]
        public async Task GetPostById_ShouldReturnBadRequest_WhenPostIdIsInvalid()
        {
            // Arrange
            SetupMockUser();
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
            SetupMockUser();
            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            var postId = 3;
            var title = "Title Lorem ipsum dolor sit amet 3";
            var expectedPost = posts
                .FirstOrDefault(p => p.Title == title);

            // Act
            var response = await _client.GetAsync
                (string.Format(Posts.GetById, postId));
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PostAdminDetailsDto>>();
            response.EnsureSuccessStatusCode();

            // Assert            
            Assert.True(content!.Success);
            var data = content.Data!;

            Assert.Equal(postId, content.Data.Id);
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
            SetupMockUser();
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
            SetupMockUser();

            var invalidPost = new Post
            {
                Title = "",
                Content = "Valid content",
                Author = "Valid author"
            };
            var content = HttpHelper.GetJsonHttpContent(invalidPost);

            // Act
            var response = await _client.PostAsync(Posts.Base, content);

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
            SetupMockUser();
            const string ExpectedSafeContent = "Test content";
            const string HackContent = "Test content<script>Hack code</script>";

            var culinaryCategories = TestDataHelper.GetCulinaryCategories();
            await SeedCategoriesAsync(culinaryCategories);

            var categoryListFromDb = await GetCategoriesFromDbAsync();
            var postToCreate = TestDataHelper.GetSinglePost(categoryListFromDb, includeId: false);
            postToCreate.Content = HackContent;

            var content = HttpHelper.GetJsonHttpContent(postToCreate);

            // Act
            var response = await _client.PostAsync(Posts.Base, content);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PostAdminDetailsDto>>();

            Assert.Equal(ExpectedSafeContent, result.Data.Content);
            Assert.DoesNotContain("<script>", result.Data.Content);

            var locationHeader = response.Headers.Location?.ToString();
            Assert.NotNull(locationHeader);

            Assert.True(result.Success);
            Assert.Equal(PostM.Success.PostAddedSuccessfully, result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(postToCreate.Title, result.Data.Title);
        }

        [Fact]
        public async Task UpdatePost_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            SetupMockUser();

            var postId = 1;
            var invalidPost = new PostUpdateDto
            {
                Title = "Valid Title",
                Content = "",
                Author = "Author"
            };

            var content = HttpHelper.GetJsonHttpContent(invalidPost);
            var url = string.Format(Posts.GetById, postId);

            // Act
            var response = await _client.PutAsync(url, content);

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
            SetupMockUser();
            const string ExpectedSafeContent = "Updated test content";
            const string HackContent = "Updated test content<script>Hack code</script>";
            const string NewTitle = "Absolutely New Title";

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

            var postId = 2;
            var updateDto = TestDataHelper.GetPostUpdateDto(title: NewTitle, content: HackContent);

            var content = HttpHelper.GetJsonHttpContent(updateDto);
            var url = string.Format(Posts.GetById, postId);

            // Act                
            var response = await _client.PutAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PostAdminDetailsDto>>();

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(postId, result.Data.Id);
            Assert.Equal(ExpectedSafeContent, result.Data.Content);
            Assert.DoesNotContain("<script>", result.Data.Content);


            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var updatedInDb = await dbContext.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId);

                Assert.NotNull(updatedInDb);
                Assert.Equal(NewTitle, updatedInDb.Title);
                Assert.Equal(ExpectedSafeContent, updatedInDb.Content);
            }
        }

        [Fact]
        public async Task DeletePost_ShouldReturnBadRequest_WhenPostIdIsInvalid()
        {
            // Arrange
            SetupMockUser();

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
            SetupMockUser();

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
            SetupMockUser();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await SeedDatabaseAsync(posts, categories);

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

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var deletedPost = await dbContext.Posts.AnyAsync(p => p.Id == postId);

                Assert.False(deletedPost);
            }
        }

        [Fact]
        public async Task DeletePostAsync_ShouldRemovePostAndAssociatedComments_Cascade()
        {
            // Arrange
            SetupMockUser();

            var categories = TestDataHelper.GetCulinaryCategories();           
            var posts = TestDataHelper.GetPostsWithComments( count: 1, categories, commentCount: 3);
            await SeedDatabaseAsync(posts, categories);

            var targetPost = posts.First();
            var postId = targetPost.Id;
            var url = string.Format(Posts.GetById, postId);

            // Act
            var response = await _client.DeleteAsync(url);
            response.EnsureSuccessStatusCode();

            // Assert
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var postExists = await dbContext.Posts.AnyAsync(p => p.Id == postId);
                Assert.False(postExists);
                
                var orphanCommentsExist = await dbContext.Comments.AnyAsync(c => c.PostId == postId);
                Assert.False(orphanCommentsExist);
            }
        }

        private async Task SeedDatabaseAsync(IEnumerable<Post> posts, IEnumerable<Category> categories)
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
                await dbContext.SaveChangesAsync();

                await dbContext.Posts.AddRangeAsync(posts);
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task SeedCategoriesAsync(IEnumerable<Category> categories)
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                if (await dbContext.Database.EnsureCreatedAsync())
                {
                    await dbContext.Categories.AddRangeAsync(categories);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        private async Task<List<Category>> GetCategoriesFromDbAsync()
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Categories.ToListAsync();
        }

        private void SetupMockUser()
        {
            var adminClaims = new[]
                {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _fixture.SetCurrentUser(adminPrincipal);
        }
    }
}
