using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    [Collection("SharedDatabase")]
    public class AuthControllerTests
    {
        private readonly HttpClient? _client;
        private readonly IServiceProvider? _services;
        private readonly ServiceTestFixture _fixture;

        public AuthControllerTests(ServiceTestFixture fixture)
        {
            _client = fixture.Client;
            _services = fixture.Services;
            _fixture = fixture;
        }

        [Fact]
        public async Task OnRegister_ShouldReturnBadRequest_WhenDataIsInvalid()
        {
            // Arrange            
            var invalidData = new RegisterUserDto { Email = "not-an-email" };
            var url = Authentication.Register;

            // Act
            var response = await _client!.PostAsJsonAsync(url, invalidData);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(content);
            Assert.False(content.Success);
            Assert.Equal(Global.Validation.ValidationFailed, content.Message);
            Assert.NotNull(content.Errors);
            Assert.True(content.Errors.Any());
        }

        [Fact]
        public async Task OnRegister_ShouldReturnSuccessResponse_IfUserRegisterSuccessfully()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            var newUser = new RegisterUserDto { UserName = "Bob", Email = "bob@test.com", Password = "-Rtyuehe6" };
            var url = Authentication.Register;

            // Act     
            var response = await _client!.PostAsJsonAsync(url, newUser);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterUserDto>>();

            // Assert
            Assert.True(result!.Success);
            Assert.Equal(Auth.Registration.Success.RegisterOk, result.Message);

            var userInDb = await _fixture.ExecuteInScopeAsync(db =>
                db.Users.AsNoTracking().FirstOrDefaultAsync(p => p.Email == newUser.Email));

            Assert.NotNull(userInDb);
            Assert.Equal(newUser.UserName, userInDb.UserName);
            Assert.Equal(newUser.Email, userInDb.Email);
        }

        [Fact]
        public async Task OnLogin_ShouldReturnBadRequest_WhenCredentialsAreInvalid()
        {
            // Arrange
            var invalidCreds = new LoginUserDto { UserName = "" };
            var url = Authentication.Login;

            // Act
            var response = await _client!.PostAsJsonAsync(url, invalidCreds);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.False(content!.Success);
            Assert.Equal(Global.Validation.ValidationFailed, content.Message);
            Assert.NotNull(content.Errors);
            Assert.True(content.Errors.Any());
        }

        [Theory]
        [InlineData(TestUserData.AdminUserName, TestUserData.AdminPassword)]
        [InlineData(TestUserData.ContributorUserName, TestUserData.ContributorPassword)]
        public async Task OnLogin_ShouldAuthenticateUser_GenerateTokenSuccessfully(string userName, string password)
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services!.SeedDefaultUsersAsync();

            var loginUser = new LoginUserDto { UserName = userName, Password = password };
            var url = Authentication.Login;

            // Act
            var loginResponse = await _client!.PostAsJsonAsync(url, loginUser);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoggedInUserDto>>();

            // Assert            
            Assert.True(loginResult!.Success);
            Assert.Equal(string.Format(Auth.LoginM.Success.LoginSuccess), loginResult.Message);
            Assert.NotNull(loginResult.Data.Token);
            Assert.NotEmpty(loginResult.Data.Token);

            var tokenParts = loginResult.Data.Token.Split('.');
            Assert.Equal(3, tokenParts.Length);
        }

        [Fact]
        public async Task GetAuthors_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange            
            _client!.DefaultRequestHeaders.Remove(TestUserData.TestUserHeader);
            var url = Authentication.Authors;

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
            var url = Authentication.Authors;

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
            var url = Authentication.Authors;

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
