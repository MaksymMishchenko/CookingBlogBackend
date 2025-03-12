using Microsoft.AspNetCore.Identity;
using Moq;
using PostApiService.Interfaces;
using PostApiService.Models;
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
                (_mockUserManager.Object);
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

            // Act
            await _authService.RegisterUserAsync(registerUser);

            // Assert
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>(), registerUser.Password), Times.Once);
        }
    }
}

