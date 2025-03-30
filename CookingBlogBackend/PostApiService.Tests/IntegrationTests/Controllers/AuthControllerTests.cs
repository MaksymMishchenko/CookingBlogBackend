using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Exceptions;
using PostApiService.Models;
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
        public async Task OnRegister_ShouldReturnSuccessResponse_IfUserRegisterSuccessfully()
        {
            // Arrange
            var newUser = new RegisterUser { UserName = "Bob", Email = "bob@test.com", Password = "-Rtyuehe6" };
            var content = HttpHelper.GetJsonHttpContent(newUser);

            // Act            ;
            var response = await _client!.PostAsync("/api/auth/register", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterUser>>();

            // Assert
            Assert.True(result.Success);
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
        public async Task OnLogin_ShouldAuthenticateUser_GenerateTokenSuccessfully()
        {
            // Arrange            
            var loginUser = new LoginUser { UserName = "cont", Password = "-Rtyuehe2" };

            // Act
            var loginContent = HttpHelper.GetJsonHttpContent(loginUser);
            var loginResponse = await _client!.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

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
