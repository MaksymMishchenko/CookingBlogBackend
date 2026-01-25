using PostApiService.Controllers;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly IAuthService _mockAuthService;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _mockAuthService = Substitute.For<IAuthService>();
            _authController = new AuthController(_mockAuthService);
        }

        [Fact]
        public async Task OnRegisterUser_ShouldReturnConflict_WhenUsernameAlreadyExists()
        {
            // Arrange
            var newUser = AuthTestData.CreateRegisterUserDto();
            var ct = CancellationToken.None;

            var conflictResult = Result<RegisteredUserDto>.Conflict(
                Auth.Registration.Errors.UserAlreadyExists,
                Auth.Registration.Errors.UserAlreadyExistsCode);

            _mockAuthService.RegisterUserAsync(newUser, ct)
                .Returns(Task.FromResult(conflictResult));

            // Act
            var result = await _authController.RegisterUser(newUser, ct);

            // Assert
            var conflictRequestResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(conflictRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(Auth.Registration.Errors.UserAlreadyExists, response.Message);

            await _mockAuthService.Received(1).RegisterUserAsync(newUser, ct);
        }

        [Fact]
        public async Task OnRegisterUser_ShouldReturnConflict_WhenEmailAlreadyExists()
        {
            // Arrange
            var newUser = AuthTestData.CreateRegisterUserDto();
            var ct = CancellationToken.None;

            var conflictResult = Result<RegisteredUserDto>.Conflict(
                Auth.Registration.Errors.UserAlreadyExists,
                Auth.Registration.Errors.UserAlreadyExistsCode);

            _mockAuthService.RegisterUserAsync(newUser, ct)
                .Returns(Task.FromResult(conflictResult));

            // Act
            var result = await _authController.RegisterUser(newUser, ct);

            // Assert
            var conflictRequestResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(conflictRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(Auth.Registration.Errors.UserAlreadyExists, response.Message);

            await _mockAuthService.Received(1).RegisterUserAsync(newUser, ct);
        }

        [Fact]
        public async Task OnRegisterUser_ShouldReturnOk_IfUserRegisterSuccessfully()
        {
            // Arrange            
            var newUser = AuthTestData.CreateRegisterUserDto();
            var registeredUserDto = AuthTestData.CreateRegisteredUserDto();
            var ct = CancellationToken.None;

            var createdResult = Result<RegisteredUserDto>.Created(
                registeredUserDto, Auth.Registration.Success.RegisterOk);

            _mockAuthService.RegisterUserAsync(newUser, ct).Returns(createdResult);

            // Act
            var result = await _authController.RegisterUser(newUser, ct);

            // Assert
            var okRequestResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<RegisteredUserDto>>(okRequestResult.Value);
            Assert.True(response.Success);
            Assert.Equal(string.Format(Auth.Registration.Success.RegisterOk),
                response.Message);

            await _mockAuthService.Received(1)
                .RegisterUserAsync(newUser, ct);
        }

        [Fact]
        public async Task OnLoginUser_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginDto = AuthTestData.CreateUserLoginDto();
            var errorResult = Result<LoggedInUserDto>.Unauthorized(Auth.LoginM.Errors.InvalidCredentials);

            _mockAuthService.AuthenticateAsync(loginDto, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(errorResult));

            // Act
            var result = await _authController.LoginUserAsync(loginDto);

            // Assert            
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(unauthorizedResult.Value);

            Assert.False(response.Success);
            Assert.Equal(Auth.LoginM.Errors.InvalidCredentials, response.Message);

            await _mockAuthService.Received(1).AuthenticateAsync(loginDto, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task OnLoginUser_ShouldReturnOk_IfUserLoginSuccessfully()
        {
            // Arrange                        
            var loginDto = AuthTestData.CreateUserLoginDto();
            var loggedInDto = AuthTestData.CreateLoggedInUserDto();
            var ct = CancellationToken.None;

            var responseDto = Result<LoggedInUserDto>.Success(loggedInDto, string.Format(
                Auth.LoginM.Success.LoginSuccess, loggedInDto.UserName));

            _mockAuthService.AuthenticateAsync(loginDto, ct)
                .Returns(responseDto);

            // Act
            var result = await _authController.LoginUserAsync(loginDto, ct);

            // Assert
            var okRequestResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<LoggedInUserDto>>(okRequestResult.Value);
            Assert.True(response.Success);
            Assert.Equal(loggedInDto.Token, response.Data.Token);
            Assert.Equal(string.Format(Auth.LoginM.Success.LoginSuccess), response.Message);

            await _mockAuthService.Received(1)
                .AuthenticateAsync(loginDto, ct);
        }
    }
}
