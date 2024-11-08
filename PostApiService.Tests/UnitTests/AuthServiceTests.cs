using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using PostApiService.Interfaces;
using PostApiService.Models;
using AuthService = PostApiService.Services.AuthService;

namespace PostApiService.Tests.UnitTests
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<SignInManager<IdentityUser>> _mockSignInManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockUserManager = new Mock<UserManager<IdentityUser>>(Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);
            _mockSignInManager = new Mock<SignInManager<IdentityUser>>(_mockUserManager.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(), null, null, null, null);
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockTokenService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task LoginAsync_SuccessfulLogin_ReturnsToken()
        {
            // Arrange
            var loginModel = new LoginModel { Username = "validuser", Password = "correctpassword" };
            var user = new IdentityUser { UserName = "validuser" };

            _mockUserManager.Setup(x => x.FindByNameAsync(loginModel.Username)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.PasswordSignInAsync(user, loginModel.Password, false, true)).ReturnsAsync(SignInResult.Success);

            var expectedToken = "mocked-token";
            var expectedExpiration = DateTime.Now.AddHours(1);
            _mockTokenService.Setup(x => x.GenerateJwtToken(It.IsAny<IdentityUser>())).Returns((expectedToken, expectedExpiration));

            // Act
            var result = await _authService.LoginAsync(loginModel);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedToken, result.Token);
            Assert.Equal(expectedExpiration, result.Expiration);
        }

        [Fact]
        public async Task LoginAsync_UserDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var loginModel = new LoginModel { Username = "invaliduser", Password = "correctpassword" };
            _mockUserManager.Setup(x => x.FindByNameAsync(loginModel.Username)).ReturnsAsync((IdentityUser?)null);

            // Act
            var result = await _authService.LoginAsync(loginModel);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Token);
            Assert.Equal(DateTime.MinValue, result.Expiration);
        }
    }
}

