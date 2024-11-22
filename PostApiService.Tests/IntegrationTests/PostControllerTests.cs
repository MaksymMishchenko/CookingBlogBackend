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

        }
    }
}
