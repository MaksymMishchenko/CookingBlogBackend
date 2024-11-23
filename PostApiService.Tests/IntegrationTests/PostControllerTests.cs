using PostApiService.Models;
using PostApiService.Tests.Helper;
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
        public async Task OnGetPosts_WhenExecuteApi_ShouldReturnExpectedPosts()
        {
            // Arrange            

            // Act
            var response = await _factory.HttpClient.GetAsync(HttpHelper.Urls.GetAllPosts);
            var result = await response.Content.ReadFromJsonAsync<List<Post>>();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.Equal(_factory.InitializePostData, result.Count);
        }        
    }
}
