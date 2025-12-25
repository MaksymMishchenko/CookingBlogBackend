using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    public class AuthControllerTests : IClassFixture<AuthFixture>
    {
        private readonly HttpClient? _client;
        private readonly IServiceProvider? _services;
        public AuthControllerTests(AuthFixture fixture)
        {
            _client = fixture.Client;
            _services = fixture.Services;
        }

        [Fact]
        public async Task OnRegister_ShouldReturnBadRequest_WhenDataIsInvalid()
        {
            // Arrange
            var invalidData = new RegisterUser { Email = "not-an-email" };

            var url = HttpHelper.Urls.Auth.Register;

            // Act
            var response = await _client!.PostAsJsonAsync(url, invalidData);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(content);
            Assert.False(content.Success);
            Assert.Equal(ResponseErrorMessages.ValidationFailed, content.Message);
            Assert.NotNull(content.Errors);
            Assert.True(content.Errors.Any());
        }

        [Fact]
        public async Task OnRegister_ShouldReturnSuccessResponse_IfUserRegisterSuccessfully()
        {
            // Arrange
            var newUser = new RegisterUser { UserName = "Bob", Email = "bob@test.com", Password = "-Rtyuehe6" };
            var content = HttpHelper.GetJsonHttpContent(newUser);

            var url = HttpHelper.Urls.Auth.Register;

            // Act     
            var response = await _client!.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterUser>>();

            // Assert
            Assert.True(result!.Success);
            Assert.Equal(string.Format(RegisterSuccessMessages.RegisterOk,
                newUser.UserName), result.Message);

            using (var scope = _services!.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var userInDb = await userManager.FindByEmailAsync(newUser.Email);

                Assert.NotNull(userInDb);
                Assert.Equal(newUser.UserName, userInDb.UserName);
                Assert.Equal(newUser.Email, userInDb.Email);
            }
        }

        [Fact]
        public async Task OnLogin_ShouldReturnBadRequest_WhenCredentialsAreInvalid()
        {
            // Arrange
            var invalidCreds = new LoginUser { UserName = "" };
            var url = HttpHelper.Urls.Auth.Login;

            // Act
            var response = await _client!.PostAsJsonAsync(url, invalidCreds);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.False(content!.Success);
            Assert.Equal(ResponseErrorMessages.ValidationFailed, content.Message);
            Assert.NotNull(content.Errors);
            Assert.True(content.Errors.Any());
        }

        [Fact]
        public async Task OnLogin_ShouldAuthenticateUser_GenerateTokenSuccessfully()
        {
            // Arrange            
            var loginUser = new LoginUser { UserName = "cont", Password = "-Rtyuehe2" };
            var loginContent = HttpHelper.GetJsonHttpContent(loginUser);

            var url = HttpHelper.Urls.Auth.Login;

            // Act
            var loginResponse = await _client!.PostAsync(url, loginContent);

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginUser>>();

            // Assert
            Assert.True(loginResult!.Success);
            Assert.Equal(string.Format(AuthSuccessMessages.LoginSuccess,
                loginUser.UserName), loginResult.Message);
            Assert.NotNull(loginResult.Token);
            Assert.NotEmpty(loginResult.Token);

            var tokenParts = loginResult.Token.Split('.');
            Assert.Equal(3, tokenParts.Length);
        }
    }
}
