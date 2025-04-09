using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Repositories;
using System.Security.Claims;
using AuthService = PostApiService.Services.AuthService;

namespace PostApiService.Tests.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly IAuthRepository _mockAuthRepository;
        private readonly IHttpContextAccessor _mockHttpContextAccessor;
        private readonly ITokenService _mockTokenService;
        private readonly AuthService _authService;
        public AuthServiceTests()
        {
            _mockAuthRepository = Substitute.For<IAuthRepository>();
            _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            _mockTokenService = Substitute.For<ITokenService>();
            _authService = new AuthService
                (_mockAuthRepository, _mockTokenService);
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

            _mockAuthRepository.FindByNameAsync(registerUser.UserName)
            .Returns(Task.FromResult((IdentityUser)null));

            _mockAuthRepository.FindByEmailAsync(registerUser.Email)
            .Returns(Task.FromResult((IdentityUser)null));

            var identityUser = new IdentityUser
            {
                UserName = registerUser.UserName,
                Email = registerUser.Email
            };

            var createResult = IdentityResult.Success;
            _mockAuthRepository.CreateAsync(Arg.Any<IdentityUser>(), registerUser.Password)
                .Returns(Task.FromResult(createResult));

            var claimResult = IdentityResult.Success;
            _mockAuthRepository.AddClaimAsync(Arg.Any<IdentityUser>(), Arg.Any<Claim>())
                .Returns(Task.FromResult(claimResult));

            // Act
            await _authService.RegisterUserAsync(registerUser);

            // Assert
            await _mockAuthRepository.Received(1)
                .CreateAsync(Arg.Any<IdentityUser>(), registerUser.Password);
            await _mockAuthRepository.Received(1).Received(1).
                AddClaimAsync(Arg.Any<IdentityUser>(), Arg.Any<Claim>());
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

            _mockAuthRepository.FindByNameAsync(loginUser.UserName)
            .Returns(Task.FromResult(identityUser));

            _mockAuthRepository.CheckPasswordAsync(Arg.Any<IdentityUser>(), loginUser.Password)
                .Returns(Task.FromResult(true));

            // Act
            var result = await _authService.LoginAsync(loginUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(loginUser.UserName, result.UserName);
            await _mockAuthRepository.Received(1)
                 .FindByNameAsync(loginUser.UserName);
            await _mockAuthRepository.Received(1)
                .CheckPasswordAsync(identityUser, loginUser.Password);
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

            _mockHttpContextAccessor.HttpContext.Returns(httpContext);

            _mockAuthRepository.GetUserAsync()
                .Returns(Task.FromResult(user));

            // Act
            var result = await _authService.GetCurrentUserAsync();

            // Assert
            Assert.Equal("testuser", result.UserName);
            Assert.Equal("test@example.com", result.Email);
        }

        //[Fact]
        //public async Task GenerateTokenString_ShouldReturnToken_WhenUserExists()
        //{
        //    // Arrange
        //    var userName = "admin";
        //    var user = new IdentityUser { UserName = userName };

        //    var claims = new List<Claim>
        //    {
        //        new Claim("permissions", "[1,2,3]"),
        //    };

        //    var rolesFromManager = new List<string> { TS.Roles.Admin }; // Ролі користувача            

        //    var expectedToken = "generated_token";

        //    _mockUserManager.Setup(x => x.FindByNameAsync(userName))
        //                    .ReturnsAsync(user);

        //    _mockUserManager.Setup(x => x.GetClaimsAsync(user))
        //                    .ReturnsAsync(claims);

        //    _mockUserManager.Setup(x => x.GetRolesAsync(user))
        //                    .ReturnsAsync(rolesFromManager);

        //    _mockTokenService.Setup(x => x.GenerateTokenString(It.IsAny<IEnumerable<Claim>>()))
        //                     .Returns(expectedToken);

        //    // Act
        //    var result = await _authService.GenerateTokenString(user);

        //    // Assert
        //    Assert.Equal(expectedToken, result);

        //    _mockUserManager.Verify(x => x.FindByNameAsync(userName), Times.Once);
        //    _mockUserManager.Verify(x => x.GetClaimsAsync(user), Times.Once);
        //    _mockUserManager.Verify(x => x.GetRolesAsync(user), Times.Once);

        //    _mockTokenService.Verify(x => x.GenerateTokenString(It.Is<IEnumerable<Claim>>(claims =>
        //        claims.Any(c => c.Type == ClaimTypes.Name && c.Value == userName) &&
        //        claims.Count(c => c.Type == "permissions") == 3 &&
        //        claims.Any(c => c.Type == "permissions" && c.Value == "1") &&
        //        claims.Any(c => c.Type == "permissions" && c.Value == "2") &&
        //        claims.Any(c => c.Type == "permissions" && c.Value == "3") &&
        //        claims.Any(c => c.Type == ClaimTypes.Role && c.Value == TS.Roles.Admin)
        //    )), Times.Once);
        //}
    }
}

