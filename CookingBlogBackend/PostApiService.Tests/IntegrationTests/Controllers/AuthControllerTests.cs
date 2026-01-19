using PostApiService.Models.Common;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    [Collection("SharedDatabase")]
    public class AuthControllerTests
    {
        private readonly HttpClient? _client;
        private readonly IServiceProvider? _services;
        private readonly BaseTestFixture _fixture;

        public AuthControllerTests(BaseTestFixture fixture)
        {
            _client = fixture.Client;
            _services = fixture.Services;
            _fixture = fixture;
        }

        [Fact]
        public async Task OnRegister_ShouldReturnBadRequest_WhenDataIsInvalid()
        {
            // Arrange            
            var invalidData = new RegisterUser { Email = "not-an-email" };

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

            var newUser = new RegisterUser { UserName = "Bob", Email = "bob@test.com", Password = "-Rtyuehe6" };
            var url = Authentication.Register;

            // Act     
            var response = await _client!.PostAsJsonAsync(url, newUser);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterUser>>();

            // Assert
            Assert.True(result!.Success);
            Assert.Equal(string.Format(Auth.Registration.Success.RegisterOk,
                newUser.UserName), result.Message);

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
            var invalidCreds = new LoginUser { UserName = "" };
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
            var loginUser = new LoginUser { UserName = userName, Password = password };
            var url = Authentication.Login;

            // Act
            var loginResponse = await _client!.PostAsJsonAsync(url, loginUser);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginUser>>();

            // Assert            
            Assert.True(loginResult!.Success);
            Assert.Equal(string.Format(Auth.LoginM.Success.LoginSuccess,
                loginUser.UserName), loginResult.Message);
            Assert.NotNull(loginResult.Token);
            Assert.NotEmpty(loginResult.Token);

            var tokenParts = loginResult.Token.Split('.');
            Assert.Equal(3, tokenParts.Length);
        }
    }
}
