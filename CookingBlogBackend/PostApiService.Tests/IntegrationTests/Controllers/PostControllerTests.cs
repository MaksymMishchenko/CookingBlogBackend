using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests
{
    public class PostControllerTests : IClassFixture<PostControllerFixture>
    {
        private readonly PostControllerFixture _factory;
        private readonly HttpClient _client;
        public PostControllerTests(PostControllerFixture factory)
        {
            _factory = factory;
            _client = factory.Client;
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnSeededPosts_WithComments()
        {
            // Arrange
            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            // Act
            var response = await _client.GetAsync(HttpHelper.Urls.GetAllPosts);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();

            response.EnsureSuccessStatusCode();

            // Assert
            Assert.True(content.Success);
            Assert.Equal(string.Format(SuccessMessages.PostsRetrievedSuccessfully,
                content.DataList.Count), content.Message);
            Assert.Equal(posts.Count, content.DataList.Count);

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
        public async Task GetPosts_Pagination_ShouldReturnCorrectPageResults()
        {
            // Arrange
            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            int pageSize = 5;
            int totalPosts = posts.Count;
            int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            // Act
            var response = await _factory.Client.GetAsync
                (string.Format(HttpHelper.Urls.PaginatedPostsUrl, 1, pageSize));

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(content);
            Assert.Equal(pageSize, content.DataList.Count);
            Assert.True(content.DataList.First().PostId > 0);

            // Act
            response = await _factory.Client.GetAsync
                (string.Format(HttpHelper.Urls.PaginatedPostsUrl, totalPages, pageSize));

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            content = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(content);
            Assert.InRange(content.DataList.Count, 0, pageSize);
            Assert.True(content.DataList.Last().PostId > 0);
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
            Assert.Equal(string.Format(SuccessMessages.PostRetrievedSuccessfully,
                content.Data.PostId), content.Message);

            Assert.Equal(postId, content.Data.PostId);
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

            var locationHeader = response.Headers.Location?.ToString();
            Assert.NotNull(locationHeader);
            Assert.StartsWith("http://localhost/api/Posts/GetPost/", locationHeader);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(SuccessMessages.PostAddedSuccessfully, result.Message);
            Assert.True(result.EntityId > 0);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturn200Ok_IfPostUpdatedSuccessfully()
        {
            // Arrange
            var posts = TestDataHelper.GetPostsWithComments();
            await SeedDatabaseAsync(posts);

            var postId = 2;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var existingPost = await dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == postId);

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
                    (SuccessMessages.PostUpdatedSuccessfully, postId), response.Message);
                Assert.Equal(postId, response.EntityId);

                dbContext.ChangeTracker.Clear();

                var updatedPost = await dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
                Assert.Equal(existingPost.Title, updatedPost.Title);
                Assert.Equal(existingPost.Description, updatedPost.Description);
            }
        }

        private async Task SeedDatabaseAsync(IEnumerable<Post> posts)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();

                await dbContext.Posts.AddRangeAsync(posts);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
