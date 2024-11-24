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
        public async Task OnGetPosts_WhenExecuteController_ShouldReturnTheExpectedPost()
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
        public async Task OnAddPost_WhenExecuteController_ShouldStoreInDb()
        {
            // Arrange
            var newPost = DataFixture.GetPost(true);

            // Act            
            var request = await _factory.HttpClient.PostAsync(HttpHelper.Urls.AddPost, HttpHelper.GetJsonHttpContent(newPost));
            var response = await _factory.HttpClient.GetAsync($"{HttpHelper.Urls.GetPostById}/{_factory.InitializePostData + 1}");
            var result = await response.Content.ReadFromJsonAsync<Post>();

            // Assert
            request.EnsureSuccessStatusCode();
            response.EnsureSuccessStatusCode();

            Assert.Equal(newPost.Title, result.Title);
            Assert.Equal(newPost.Description, result.Description);
            Assert.Equal(newPost.Slug, result.Slug);
            Assert.Equal(newPost.ImageUrl, result.ImageUrl);
            Assert.Equal(newPost.MetaTitle, result.MetaTitle);
            Assert.Equal(newPost.MetaDescription, result.MetaDescription);
            Assert.NotNull(result.Comments);
            Assert.NotEmpty(result.Comments);
        }
    }
}
