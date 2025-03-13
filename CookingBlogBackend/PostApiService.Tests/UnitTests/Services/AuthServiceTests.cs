using Microsoft.AspNetCore.Identity;
using Moq;
using PostApiService.Interfaces;
using PostApiService.Models;
using System.Security.Claims;
using AuthService = PostApiService.Services.AuthService;

namespace PostApiService.Tests.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly AuthService _authService;
        public AuthServiceTests()
        {
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
         Mock.Of<IUserStore<IdentityUser>>(),
         null, null, null, null, null, null, null, null
         );

            _mockTokenService = new Mock<ITokenService>();
            _authService = new AuthService
                (_mockUserManager.Object, _mockTokenService.Object);
        }

        [Fact]
        public async Task RegisterUser_ShouldRegisterNewUser()
        {
            // Arrange
            var registerUser = new RegisterUser
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "P@ssw0rd"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync(registerUser.UserName))
            .ReturnsAsync((IdentityUser)null);

            _mockUserManager.Setup(x => x.FindByEmailAsync(registerUser.Email))
            .ReturnsAsync((IdentityUser)null);

            var identityUser = new IdentityUser
            {
                UserName = registerUser.UserName,
                Email = registerUser.Email
            };

            var createResult = IdentityResult.Success;
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), registerUser.Password))
                .ReturnsAsync(createResult);

            var claimResult = IdentityResult.Success;
            _mockUserManager.Setup(x => x.AddClaimAsync(It.IsAny<IdentityUser>(), It.IsAny<Claim>()))
                .ReturnsAsync(claimResult);

            // Act
            await _authService.RegisterUserAsync(registerUser);

            // Assert
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>(), registerUser.Password), Times.Once);
            _mockUserManager.Verify(x => x.AddClaimAsync(It.IsAny<IdentityUser>(), It.IsAny<Claim>()), Times.Once);
        }

        [Fact]
        public async Task LoginUser_ShouldLoginUserSuccessfully()
        {
            // Arrange
            var loginUser = new LoginUser
            {
                UserName = "newuser",
                Password = "P@ssw0rd"
            };

            var identityUser = new IdentityUser
            {
                UserName = loginUser.UserName,
                Email = "newuser@example.com",
            };

            _mockUserManager.Setup(x => x.FindByNameAsync(loginUser.UserName))
            .ReturnsAsync(identityUser);

            _mockUserManager.Setup(x => x.CheckPasswordAsync(It.IsAny<IdentityUser>(), loginUser.Password))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.LoginAsync(loginUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(loginUser.UserName, result.UserName);
            _mockUserManager.Verify(x => x.FindByNameAsync(loginUser.UserName), Times.Once);
            _mockUserManager.Verify(x => x.CheckPasswordAsync(identityUser, loginUser.Password), Times.Once);
        }
    }
}

