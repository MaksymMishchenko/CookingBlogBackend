﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.TypeSafe;
using System.Security.Claims;
using AuthService = PostApiService.Services.AuthService;

namespace PostApiService.Tests.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<IHttpContextAccessor> _mockContextAccessor;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly AuthService _authService;
        public AuthServiceTests()
        {
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
         Mock.Of<IUserStore<IdentityUser>>(),
         null, null, null, null, null, null, null, null
         );

            _mockContextAccessor = new Mock<IHttpContextAccessor>();
            _mockTokenService = new Mock<ITokenService>();
            _authService = new AuthService
                (_mockUserManager.Object, _mockContextAccessor.Object, _mockTokenService.Object);
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

        [Fact]
        public async Task GetCurrentUserAsync_ReturnsUser_WhenUserIsAuthenticated()
        {
            // Arrange
            var user = new IdentityUser { UserName = "testuser", Email = "test@example.com" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _mockContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            _mockUserManager
                .Setup(x => x.GetUserAsync(It.Is<ClaimsPrincipal>(c => c == claimsPrincipal)))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.GetCurrentUserAsync();

            // Assert
            Assert.Equal("testuser", result.UserName);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GenerateTokenString_ShouldReturnToken_WhenUserExists()
        {
            // Arrange
            var userName = "admin";
            var user = new IdentityUser { UserName = userName };

            var claims = new List<Claim>
            {
                new Claim("permissions", "[1,2,3]"),
            };

            var rolesFromManager = new List<string> { TS.Roles.Admin }; // Ролі користувача            

            var expectedToken = "generated_token";

            _mockUserManager.Setup(x => x.FindByNameAsync(userName))
                            .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.GetClaimsAsync(user))
                            .ReturnsAsync(claims);

            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                            .ReturnsAsync(rolesFromManager);

            _mockTokenService.Setup(x => x.GenerateTokenString(It.IsAny<IEnumerable<Claim>>()))
                             .Returns(expectedToken);

            // Act
            var result = await _authService.GenerateTokenString(user);

            // Assert
            Assert.Equal(expectedToken, result);

            _mockUserManager.Verify(x => x.FindByNameAsync(userName), Times.Once);
            _mockUserManager.Verify(x => x.GetClaimsAsync(user), Times.Once);
            _mockUserManager.Verify(x => x.GetRolesAsync(user), Times.Once);

            _mockTokenService.Verify(x => x.GenerateTokenString(It.Is<IEnumerable<Claim>>(claims =>
                claims.Any(c => c.Type == ClaimTypes.Name && c.Value == userName) &&
                claims.Count(c => c.Type == "permissions") == 3 &&
                claims.Any(c => c.Type == "permissions" && c.Value == "1") &&
                claims.Any(c => c.Type == "permissions" && c.Value == "2") &&
                claims.Any(c => c.Type == "permissions" && c.Value == "3") &&
                claims.Any(c => c.Type == ClaimTypes.Role && c.Value == TS.Roles.Admin)
            )), Times.Once);
        }
    }
}

