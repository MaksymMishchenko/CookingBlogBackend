using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Models;
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
        public async Task GetPostsWithTotalAsync_ShouldReturnSeededPosts_WithComments()
        {
            // Arrange            
            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            // Act            
            var response = await _client.GetAsync(HttpHelper.Urls.GetAllPosts);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();

            response.EnsureSuccessStatusCode();

            // Assert
            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.Equal(string.Format(PostSuccessMessages.PostsRetrievedSuccessfully,
                posts.Count), content.Message);

            Assert.NotNull(content.DataList);
            Assert.Equal(posts.Count, content.DataList.Count);

            Assert.Equal(posts.Count, content.TotalCount);

            Assert.All(content.DataList, post =>
            {
                var expectedPost = posts
                    .FirstOrDefault(p => p.Title == post.Title);

                Assert.NotNull(expectedPost);
                Assert.Equal(expectedPost.Title, post.Title);
                Assert.Equal(expectedPost.Description, post.Description);
                Assert.Equal(expectedPost.MetaTitle, post.MetaTitle);
                Assert.Equal(expectedPost.Author, post.Author);

                Assert.Equal(expectedPost.Comments.Count, post.Comments.Count);
                Assert.All(post.Comments, comment =>
                {
                    var expectedComment = expectedPost.Comments
                        .FirstOrDefault(c => c.Content == comment.Content);
                    Assert.NotNull(expectedComment);
                    Assert.Equal(expectedComment.Content, comment.Content);
                    Assert.Equal(expectedComment.Author, comment.Author);
                });
            });
        }

        [Fact]
        public async Task GetPostsWithTotalAsync_Pagination_ShouldReturnCorrectPageResults()
        {
            // Arrange
            int pageNumber1 = 1;
            int pageNumber2;
            int pageSize = 3;

            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            int totalPosts = posts.Count;
            pageNumber2 = (int)Math.Ceiling(totalPosts / (double)pageSize);

            // Act
            var response1 = await _client.GetAsync
                (string.Format(HttpHelper.Urls.PaginatedPostsUrl, pageNumber1, pageSize));

            // Assert (Page 1)
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            var content1 = await response1.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(content1);
            Assert.NotNull(content1.DataList);

            Assert.Equal(pageSize, content1.DataList.Count);

            Assert.Equal(totalPosts, content1.TotalCount);
            Assert.Equal(pageNumber1, content1.PageNumber);
            Assert.Equal(pageSize, content1.PageSize);

            Assert.Equal(1, content1.DataList.First().Id);
            Assert.Equal(3, content1.DataList.Last().Id);

            // Act
            var response2 = await _client.GetAsync(
                string.Format(HttpHelper.Urls.PaginatedPostsUrl, pageNumber2, pageSize)
            );

            // Assert (Page 2)
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            var content2 = await response2.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(content2);
            Assert.NotNull(content2.DataList);

            Assert.InRange(content2.DataList.Count, 1, pageSize); 
            
            Assert.Equal(totalPosts, content2.TotalCount); 
            Assert.Equal(pageNumber2, content2.PageNumber);
            Assert.Equal(pageSize, content2.PageSize);    
            
            Assert.Equal(4, content2.DataList.First().Id);
            Assert.Equal(5, content2.DataList.Last().Id);
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

            var newPost = new Post
            {
                Title = "Title Lorem ipsum dolor sit amet",
                Description = "Description lorem ipsum dolor sit amet",
                Author = "Lorem",
                Content = "Simple comtemt lorem ipsum dolor sit amet",
                ImageUrl = "http://img-0.com",
                MetaTitle = "Meta title dolor sit amet",
                MetaDescription = "Meta lorem ipsum dolor",
                Slug = "post-slug"
            };

            var content = HttpHelper.GetJsonHttpContent(newPost);

            // Act
            var response = await _client.PostAsync(HttpHelper.Urls.AddPost, content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(result);

            var locationHeader = response.Headers.Location?.ToString();
            Assert.NotNull(locationHeader);
            Assert.StartsWith($"http://localhost/api/posts/{result.EntityId}", locationHeader,
                StringComparison.OrdinalIgnoreCase);

            Assert.True(result.Success);
            Assert.Equal(PostSuccessMessages.PostAddedSuccessfully, result.Message);
            Assert.True(result.EntityId > 0);
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
                Assert.Equal(postId, response.EntityId);

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
