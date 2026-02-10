using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    public class CommentRateLimitTests : IClassFixture<RateLimitTestFixture>
    {
        private readonly RateLimitTestFixture _fixture;
        private readonly HttpClient _client;

        public CommentRateLimitTests(RateLimitTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
        }

        [Fact]
        public async Task AddComment_ShouldReturn429_WhenContributorExceedsLimit()
        {
            // Arrange            
            var options = _fixture.Services!.GetRequiredService<IOptions<RateLimitOptions>>().Value;
            int limit = options.PermitLimit;

            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();
            _fixture.LoginAsContributor2();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var postId = posts.First().Id;
            var comment = TestDataHelper.CreateCommentRequest("Rate limit test comment");

            var url = string.Format(Comments.GetById, postId);

            // Act           
            for (int i = 0; i < limit; i++)
            {
                var response = await _client.PostAsJsonAsync(url, comment);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            var rejectedResponse = await _client.PostAsJsonAsync(url, comment);

            // Assert
            Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);

            var result = await rejectedResponse.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.Equal(RateLimitOptions.Errors.ErrorCode, result.ErrorCode);
            Assert.Contains("limit of actions", result.Message);
        }

        [Fact]
        public async Task AddComment_ShouldAlwaysReturn200_WhenAdminExceedsLimit()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();
            _fixture.LoginAsAdmin();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);
            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var postId = posts.First().Id;
            var comment = new CommentCreateDto { Content = "Admin is god mode" };
            var url = string.Format(Comments.GetById, postId);

            int limitPlusOne = 4;

            // Act & Assert
            for (int i = 0; i < limitPlusOne; i++)
            {
                var response = await _client.PostAsJsonAsync(url, comment);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
