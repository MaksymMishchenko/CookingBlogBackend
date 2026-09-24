using PostApiService.Models.Common;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    [Collection("SharedDatabase")]
    public class UserControllerTests
    {
        private readonly HttpClient? _client;
        private readonly IServiceProvider? _services;
        private readonly ServiceTestFixture _fixture;

        public UserControllerTests(ServiceTestFixture fixture)
        {
            _client = fixture.Client;
            _services = fixture.Services;
            _fixture = fixture;
        }

        [Fact]
        public async Task GetAuthors_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange            
            _client!.DefaultRequestHeaders.Remove(TestUserData.TestUserHeader);
            var url = User.Authors;

            // Act
            var response = await _client!.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAuthors_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services!.SeedDefaultUsersAsync();

            _fixture.LoginAsContributor();
            var url = User.Authors;

            // Act
            var response = await _client!.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAuthors_ShouldReturnAuthorsList_WhenUserIsAdmin()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services!.SeedDefaultUsersAsync();

            _fixture.LoginAsAdmin();
            var url = User.Authors;

            // Act
            var response = await _client!.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AuthorsDto>>>();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(Auth.AdminM.Success.ContributorsRetrievedSuccessfully, result.Message);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);

            Assert.Contains(result.Data, a => a.UserName == TestUserData.AdminUserName);
        }
    }
}
