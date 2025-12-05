using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Models;
using PostApiService.Models.Dto;
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
            _client = fixture.Client;
            _services = fixture.Services;
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnPagedPostsWithTotalPostsAndCommentsCount()
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 10;
            const int ExpectedTotalPosts = 7;
            const int ExpectedCommentCountPerPost = 12;
            var url = string.Format(HttpHelper.Urls.PaginatedPostsUrl, PageNumber, PageSize);

            var posts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPosts, commentCount: ExpectedCommentCountPerPost);
            await SeedDatabaseAsync(posts);

            // Act            
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PostListDto>>();

            response.EnsureSuccessStatusCode();

            // Assert
            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.Equal(string.Format(PostSuccessMessages.PostsRetrievedSuccessfully,
                posts.Count), content.Message);

            Assert.NotNull(content.DataList);
            Assert.Equal(ExpectedTotalPosts, content.DataList.Count);

            Assert.Equal(ExpectedTotalPosts, content.TotalCount);

            Assert.All(content.DataList, (postDto, index) =>
            {
                int expectedIndex = (PageNumber - 1) * PageSize + index;
                var expectedPost = posts[expectedIndex];

                TestDataHelper.AssertPostListDtoMapping(expectedPost, postDto, ExpectedCommentCountPerPost);
            });
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_Pagination_ShouldReturnCorrectPageResults()
        {
            // Arrange
            const int PageNumber1 = 1;
            const int PageSize = 3;
            const int ExpectedCommentCountPerPost = 12;
            const int TotalPosts = 5;
            var url = string.Format(HttpHelper.Urls.PaginatedPostsUrl, PageNumber1, PageSize);

            var posts = TestDataHelper.GetPostsWithComments(
                count: TotalPosts, commentCount: ExpectedCommentCountPerPost);
            await SeedDatabaseAsync(posts);

            const int PageNumber2 = 2;

            // Act
            var response1 = await _client.GetAsync(url);

            response1.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadFromJsonAsync<ApiResponse<PostListDto>>();

            // Assert
            Assert.Equal(PageSize, content1.DataList.Count);
            Assert.Equal(TotalPosts, content1.TotalCount);

            Assert.All(content1.DataList, (postDto, index) =>
            {
                int expectedIndex = (PageNumber1 - 1) * PageSize + index;
                var expectedPost = posts[expectedIndex];
                TestDataHelper.AssertPostListDtoMapping(expectedPost, postDto, ExpectedCommentCountPerPost);
            });

            // Act
            url = string.Format(HttpHelper.Urls.PaginatedPostsUrl, PageNumber2, PageSize);
            var response2 = await _client.GetAsync(url);

            response2.EnsureSuccessStatusCode();

            var content2 = await response2.Content.ReadFromJsonAsync<ApiResponse<PostListDto>>();

            // Assert
            Assert.Equal(2, content2.DataList.Count);
            Assert.Equal(TotalPosts, content2.TotalCount);

            Assert.All(content2.DataList, (postDto, index) =>
            {
                int expectedIndex = (PageNumber2 - 1) * PageSize + index;
                var expectedPost = posts[expectedIndex];
                TestDataHelper.AssertPostListDtoMapping(expectedPost, postDto, ExpectedCommentCountPerPost);
            });
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnNotFound_WhenNoPostsExist()
        {
            // Arrange            
            const int PageNumber = 1;
            const int PageSize = 10;
            const int TotalPosts = 0;

            var posts = TestDataHelper.GetPostsWithComments(count: TotalPosts);
            await SeedDatabaseAsync(posts);

            var url = string.Format(HttpHelper.Urls.PaginatedPostsUrl, PageNumber, PageSize);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PostListDto>>();

            Assert.NotNull(content);
            Assert.False(content.Success);
            Assert.Null(content.DataList);
            Assert.Equal(string.Format(PostErrorMessages.NoPostsFound), content.Message);
            Assert.Equal(TotalPosts, content.TotalCount);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturn200ОК_WithExpectedPostAndComments()
        {
            // Arrange            
            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            var postId = 3;
            var title = "Title Lorem ipsum dolor sit amet 3";
            var expectedPost = posts
                .FirstOrDefault(p => p.Title == title);

            // Act
            var response = await _client.GetAsync
                (string.Format(HttpHelper.Urls.GetPostById, postId));
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            response.EnsureSuccessStatusCode();

            // Assert            
            Assert.True(content.Success);
            Assert.Equal(string.Format(PostSuccessMessages.PostRetrievedSuccessfully,
                content.Data.Id), content.Message);

            Assert.Equal(postId, content.Data.Id);
            Assert.Equal(expectedPost.Title, content.Data.Title);
            Assert.Equal(expectedPost.Description, content.Data.Description);
            Assert.Equal(expectedPost.MetaTitle, content.Data.MetaTitle);
            Assert.Equal(expectedPost.Author, content.Data.Author);

            var commentCount = content.Data.Comments.Count;
            Assert.Equal(expectedPost.Comments.Count, commentCount);

            Assert.All(content.Data.Comments, comment =>
            {
                var expectedComment = expectedPost.Comments
                .FirstOrDefault(c => c.Content == comment.Content &&
                c.Author == comment.Author);

                Assert.NotNull(expectedComment);
                Assert.Equal(expectedComment.Content, comment.Content);
                Assert.Equal(expectedComment.Author, comment.Author);
            });
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddPost_Return201CreatedAtAction()
        {
            // Arrange
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _fixture.SetCurrentUser(adminPrincipal);

            var postToCreate = TestDataHelper.GetSinglePost(includeId: false);

            var content = HttpHelper.GetJsonHttpContent(postToCreate);

            // Act
            var response = await _client.PostAsync(HttpHelper.Urls.AddPost, content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(result);

            var locationHeader = response.Headers.Location?.ToString();
            Assert.NotNull(locationHeader);

            Assert.True(result.Success);
            Assert.Equal(PostSuccessMessages.PostAddedSuccessfully, result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(postToCreate.Title, result.Data.Title);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturn200Ok_IfPostIsUpdatedSuccessfully()
        {
            // Arrange
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _fixture.SetCurrentUser(adminPrincipal);

            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            var postId = 2;

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var existingPost = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId);

                Assert.NotNull(existingPost);

                existingPost.Title = "Updated title";
                existingPost.Description = "Updated description";

                var content = HttpHelper.GetJsonHttpContent(existingPost);

                // Act
                var request = await _client.PutAsync(HttpHelper.Urls.UpdatePost, content);
                request.EnsureSuccessStatusCode();

                var response = await request.Content.ReadFromJsonAsync<ApiResponse<Post>>();

                // Assert                
                Assert.NotNull(response);
                Assert.True(response.Success);
                Assert.Equal(string.Format
                    (PostSuccessMessages.PostUpdatedSuccessfully, postId), response.Message);
                Assert.Equal(postId, response.Data.Id);

                dbContext.ChangeTracker.Clear();

                var updatedPost = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId);
                Assert.Equal(existingPost.Title, updatedPost.Title);
                Assert.Equal(existingPost.Description, updatedPost.Description);
            }
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn200Ok_IfPostIsDeletedSuccessfully()
        {
            // Arrange
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _fixture.SetCurrentUser(adminPrincipal);

            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            var postId = 4;

            // Act
            var request = await _client.DeleteAsync(string.Format(HttpHelper.Urls.DeletePost, postId));
            request.EnsureSuccessStatusCode();

            var response = await request.Content.ReadFromJsonAsync<ApiResponse<Post>>();

            // Assert
            Assert.True(response.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostDeletedSuccessfully, postId), response.Message);
            Assert.Equal(postId, response.EntityId);

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var deletedPost = await dbContext.Posts.AnyAsync(p => p.Id == postId);

                Assert.False(deletedPost);
            }
        }

        private async Task SeedDatabaseAsync(IEnumerable<Post> posts)
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                if (await dbContext.Database.EnsureCreatedAsync())
                {
                    await dbContext.Posts.AddRangeAsync(posts);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
