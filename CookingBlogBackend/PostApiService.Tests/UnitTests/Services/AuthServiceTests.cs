using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using System.Security.Claims;
using AuthService = PostApiService.Services.AuthService;

namespace PostApiService.Tests.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly IAuthRepository _mockAuthRepository;
        private readonly ITokenService _mockTokenService;
        private readonly AuthService _authService;
        public AuthServiceTests()
        {
            _mockAuthRepository = Substitute.For<IAuthRepository>();
            _mockTokenService = Substitute.For<ITokenService>();
            _authService = new AuthService
                (_mockAuthRepository, _mockTokenService);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnConflict_WhenUsernameExists()
        {
            // Arrange
            var registerDto = AuthTestData.CreateRegisterUserDto();

            _mockAuthRepository.FindByNameAsync(registerDto.UserName, Arg.Any<CancellationToken>())
                .Returns(new IdentityUser());

            // Act
            var result = await _authService.RegisterUserAsync(registerDto);

            // Assert
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal(Auth.Registration.Errors.UserAlreadyExists, result.Message);
            Assert.Equal(Auth.Registration.Errors.UserAlreadyExistsCode, result.ErrorCode);

            await _mockAuthRepository.Received(1).FindByNameAsync(
                registerDto.UserName, Arg.Any<CancellationToken>());
            await _mockAuthRepository.DidNotReceiveWithAnyArgs()
                .CreateAsync(default!, default!, default!);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnConflict_WhenEmailExists()
        {
            // Arrange
            var registerDto = AuthTestData.CreateRegisterUserDto();

            _mockAuthRepository.FindByNameAsync(registerDto.UserName, Arg.Any<CancellationToken>())
                .Returns((IdentityUser)null!);

            _mockAuthRepository.FindByEmailAsync(registerDto.Email, Arg.Any<CancellationToken>())
                .Returns(new IdentityUser());

            // Act
            var result = await _authService.RegisterUserAsync(registerDto);

            // Assert
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal(Auth.Registration.Errors.UserAlreadyExists, result.Message);
            Assert.Equal(Auth.Registration.Errors.UserAlreadyExistsCode, result.ErrorCode);

            await _mockAuthRepository.Received(1).FindByEmailAsync(
                registerDto.Email, Arg.Any<CancellationToken>());
            await _mockAuthRepository.DidNotReceiveWithAnyArgs()
                .CreateAsync(default!, default!, default!);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Arrange
            var registerDto = AuthTestData.CreateRegisterUserDto();
            var ct = CancellationToken.None;

            _mockAuthRepository.FindByNameAsync(registerDto.UserName, ct)
                .Returns(Task.FromResult<IdentityUser?>(null));
            _mockAuthRepository.FindByEmailAsync(registerDto.Email, ct)
                .Returns(Task.FromResult<IdentityUser?>(null));

            _mockAuthRepository.CreateAsync(Arg.Any<IdentityUser>(), registerDto.Password, ct)
                .Returns(IdentityResult.Success);

            _mockAuthRepository.AddClaimAsync(Arg.Any<IdentityUser>(), Arg.Any<Claim>(), ct)
                .Returns(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterUserAsync(registerDto, ct);

            // Assert
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.Equal(registerDto.UserName, result.Value.UserName);

            Assert.Equal(Auth.Registration.Success.RegisterOk, result.Message);

            await _mockAuthRepository.Received(1).CreateAsync(
                Arg.Is<IdentityUser>(u => u.UserName == registerDto.UserName &&
                u.Email == registerDto.Email),
                registerDto.Password, ct
            );

            await _mockAuthRepository.Received(1).AddClaimAsync(
                Arg.Any<IdentityUser>(),
                Arg.Any<Claim>(), ct
            );
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnInvalid_WhenIdentityResultFails()
        {
            // Arrange
            var registerDto = AuthTestData.CreateRegisterUserDto();

            _mockAuthRepository.FindByNameAsync(registerDto.UserName, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IdentityUser?>(null));
            _mockAuthRepository.FindByEmailAsync(registerDto.Email, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IdentityUser?>(null));

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." },
                new IdentityError { Code = "PasswordRequiresDigit", Description = "Password must have at least one digit." }
            };

            _mockAuthRepository.CreateAsync(Arg.Any<IdentityUser>(), registerDto.Password, Arg.Any<CancellationToken>())
                .Returns(IdentityResult.Failed(identityErrors.ToArray()));

            // Act
            var result = await _authService.RegisterUserAsync(registerDto);

            // Assert
            Assert.Equal(ResultStatus.Invalid, result.Status);

            Assert.Equal(Auth.Registration.Errors.DefaultRegistrationError, result.Message);
            Assert.Equal(Auth.Registration.Errors.DefaultRegistrationErrorCode, result.ErrorCode);

            await _mockAuthRepository.DidNotReceive().AddClaimAsync(
                Arg.Any<IdentityUser>(), Arg.Any<Claim>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnError_WhenClaimAssignmentFails()
        {
            // Arrange
            var registerDto = AuthTestData.CreateRegisterUserDto();

            _mockAuthRepository.FindByNameAsync(registerDto.UserName, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IdentityUser?>(null));

            _mockAuthRepository.FindByEmailAsync(registerDto.Email, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IdentityUser?>(null));

            _mockAuthRepository.CreateAsync(Arg.Any<IdentityUser>(), registerDto.Password, Arg.Any<CancellationToken>())
                .Returns(IdentityResult.Success);

            _mockAuthRepository.AddClaimAsync(
                Arg.Any<IdentityUser>(), Arg.Any<Claim>(), Arg.Any<CancellationToken>())
                .Returns(IdentityResult.Failed(new IdentityError { Description = "Database link failure" }));

            // Act
            var result = await _authService.RegisterUserAsync(registerDto);

            // Assert            
            Assert.Equal(ResultStatus.Error, result.Status);
            Assert.Equal(Auth.Registration.Errors.ClaimAssignmentFailed, result.Message);
            Assert.Equal(Auth.Registration.Errors.ClaimAssignmentFailedCode, result.ErrorCode);

            await _mockAuthRepository.Received(1)
                .AddClaimAsync(Arg.Any<IdentityUser>(), Arg.Any<Claim>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnError_WhenUserDoesNotExist()
        {
            // Arrange
            var loginDto = AuthTestData.CreateUserLoginDto();

            _mockAuthRepository.FindByNameAsync(loginDto.UserName, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IdentityUser?>(null));

            // Act
            var result = await _authService.AuthenticateAsync(loginDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(Auth.LoginM.Errors.InvalidCredentials, result.Message);
            Assert.Equal(Auth.LoginM.Errors.InvalidCredentialsErrorCode, result.ErrorCode);

            await _mockAuthRepository.DidNotReceiveWithAnyArgs().CheckPasswordAsync(default!, default!);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnError_WhenPasswordIsIncorrect()
        {
            // Arrange
            var loginDto = AuthTestData.CreateUserLoginDto();
            var identityUser = AuthTestData.CreateIdentityUser();

            _mockAuthRepository.FindByNameAsync(loginDto.UserName, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IdentityUser?>(identityUser));

            _mockAuthRepository.CheckPasswordAsync(identityUser, loginDto.Password, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(false));

            // Act
            var result = await _authService.AuthenticateAsync(loginDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(Auth.LoginM.Errors.InvalidCredentials, result.Message);
            Assert.Equal(Auth.LoginM.Errors.InvalidCredentialsErrorCode, result.ErrorCode);

            await _mockAuthRepository.Received(1).FindByNameAsync(loginDto.UserName, Arg.Any<CancellationToken>());
            await _mockAuthRepository.Received(1).CheckPasswordAsync(identityUser, loginDto.Password, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var loginDto = AuthTestData.CreateUserLoginDto();
            var identityUser = AuthTestData.CreateIdentityUser();
            var ct = CancellationToken.None;

            _mockAuthRepository.FindByNameAsync(loginDto.UserName, ct)
            .Returns(Task.FromResult<IdentityUser?>(identityUser));

            _mockAuthRepository.CheckPasswordAsync(Arg.Any<IdentityUser>(), loginDto.Password, ct)
                .Returns(Task.FromResult(true));

            _mockTokenService.GenerateTokenString(Arg.Any<IEnumerable<Claim>>())
                .Returns("fake-jwt-token");

            // Act
            var result = await _authService.AuthenticateAsync(loginDto, ct);

            // Assert
            Assert.NotNull(result);

            var data = result.Value!;
            Assert.True(result.IsSuccess);
            Assert.Equal("fake-jwt-token", data.Token);
            Assert.Equal(string.Format(Auth.LoginM.Success.LoginSuccess), result.Message);

            await _mockAuthRepository.Received(1)
                 .FindByNameAsync(loginDto.UserName, ct);
            await _mockAuthRepository.Received(1)
                .CheckPasswordAsync(identityUser, loginDto.Password, ct);
        }
    }
}