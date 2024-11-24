using PostApiService.Models;
using PostApiService.Tests.Helper;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests
{
    public class PostControllerTests : IClassFixture<WebApplicationFactoryFixture>
    {
        public WebApplicationFactoryFixture _factory;
        public PostControllerTests(WebApplicationFactoryFixture factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAllPosts_ShouldReturnAllPosts_WithoutToken()
        {
            // Arrange            

            // Act
            var response = await _factory.HttpClient.GetAsync(HttpHelper.Urls.GetAllPosts);
            var result = await response.Content.ReadFromJsonAsync<List<Post>>();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            var list = DataFixture.GetPosts(_factory.InitializePostData);
            Assert.Equal(list.Count, result.Count);
            Assert.Equal(list.First().Title, result.First().Title);
        }

        [Fact]
        public async Task AddPostWithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var post = DataFixture.GetPost(true);
            var request = new HttpRequestMessage(HttpMethod.Post, HttpHelper.Urls.AddPost);
            request.Content = HttpHelper.GetJsonHttpContent(post);

            // Act
            var response = await _factory.HttpClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


    }
}
