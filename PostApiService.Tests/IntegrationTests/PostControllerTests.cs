using PostApiService.Models;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests
{
    public class PostControllerTests : IClassFixture<WebApplicationFactoryFixture>
    {
        public WebApplicationFactoryFixture _fixture;
        public PostControllerTests(WebApplicationFactoryFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task OnGetPosts_WhenExecuteApi_ShouldReturnExpectedPosts()
        {
            // Arrange            

            // Act
            var response = await _fixture.HttpClient.GetAsync(HttpHelper.Urls.GetAllPosts);
            var result = await response.Content.ReadFromJsonAsync<List<Post>>();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(result);
            Assert.Equal(_fixture.InitializePostData, result.Count);            
        }
    }
}
