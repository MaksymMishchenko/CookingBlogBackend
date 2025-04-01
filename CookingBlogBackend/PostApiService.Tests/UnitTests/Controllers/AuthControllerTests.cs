using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PostApiService.Controllers;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _authController = new AuthController(_mockAuthService.Object);
        }

        [Fact]
        public async Task OnRegisterUser_ShouldReturnBadRequest_IfInvalidRegistrationData()
        {
            // Act
            var result = await _authController.RegisterUser(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<RegisterUser>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(RegisterErrorMessages.InvalidRegistrationData, response.Message);
        }

        [Fact]
        public async Task OnRegisterUser_ShouldReturnOk_IfUserRegisterSuccessfully()
        {
            // Arrange            
            var newUser = new RegisterUser
            {
                UserName = "correctUser",
                Email = "correctEmail@test.com",
                Password = "-Rtyuehe2-"
            };

            _mockAuthService.Setup(s => s.RegisterUserAsync(newUser)).Returns(Task.FromResult(newUser));

            // Act
            var result = await _authController.RegisterUser(newUser);

            // Assert
            var okRequestResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<RegisterUser>>(okRequestResult.Value);
            Assert.True(response.Success);
            Assert.Equal(string.Format(RegisterSuccessMessages.RegisterOk, newUser.UserName),
                response.Message);

            _mockAuthService.Verify(s => s.RegisterUserAsync(newUser), Times.Once);
        }

        [Fact]
        public async Task OnLoginUser_ShouldReturnBadRequest_IfInvalidLoginData()
        {
            // Act
            var result = await _authController.LoginUserAsync(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<LoginUser>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(AuthErrorMessages.InvalidCredentials, response.Message);
        }

        [Fact]
        public async Task OnLoginUser_ShouldReturnOk_IfUserLoginSuccessfully()
        {
            // Arrange            
            var newUser = new LoginUser
            {
                UserName = "correctUser",
                Password = "-Rtyuehe2-"
            };

            var identityUser = new IdentityUser { UserName = newUser.UserName, PasswordHash = newUser.Password };

            _mockAuthService.Setup(s => s.LoginAsync(It.Is<LoginUser>(user =>
                        user.UserName == "correctUser" &&
                        user.Password == "-Rtyuehe2-")))
                .ReturnsAsync(identityUser);

            // Act
            var result = await _authController.LoginUserAsync(newUser);

            // Assert
            var okRequestResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<LoginUser>>(okRequestResult.Value);
            Assert.True(response.Success);
            Assert.Equal(string.Format(AuthSuccessMessages.LoginSuccess, newUser.UserName),
                response.Message);

            _mockAuthService.Verify(s => s.LoginAsync(newUser), Times.Once);
        }
    }
}
