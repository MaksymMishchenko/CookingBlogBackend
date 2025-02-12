using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests
{
    public class PostControllerTests : IClassFixture<PostControllerFixture>
    {
        private readonly HttpClient _client;
        public PostControllerTests(PostControllerFixture factory)
        {
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
    }
}
