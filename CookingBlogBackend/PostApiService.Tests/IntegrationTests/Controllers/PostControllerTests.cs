using PostApiService.Exceptions;
using PostApiService.Models;
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
            // Act
            var response = await _client.GetAsync(HttpHelper.Urls.GetAllPosts);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<Post>>();

            response.EnsureSuccessStatusCode();

            // Assert
            Assert.True(content.Success);
            Assert.Equal(string.Format(SuccessMessages.PostsRetrievedSuccessfully,
                content.DataList.Count), content.Message);
            Assert.Equal(TestDataHelper.GetPostsWithComments().Count, content.DataList.Count);

            Assert.All(content.DataList, post =>
            {
                var expectedPost = TestDataHelper.GetPostsWithComments()
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
            int pageSize = 5;
            int totalPosts = TestDataHelper.GetPostsWithComments().Count;
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
            var postId = 3;
            var title = "Title Lorem ipsum dolor sit amet 3";
            var expectedPost = TestDataHelper.GetPostsWithComments()
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
    }
}
