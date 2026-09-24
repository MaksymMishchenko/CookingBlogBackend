using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Tests.IntegrationTests.Services
{
    [Collection("SharedDatabase")]
    public class AuthServiceIntegrationTests
    {
        private readonly ServiceTestFixture _fixture;

        public AuthServiceIntegrationTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldRegisterUser_AndAssignClaims()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            var (service, _, _) = _fixture.GetScopedService<IAuthService>();
            var userManagerBefore = _fixture.GetUserManager();
            int initialCount = await userManagerBefore.Users.CountAsync();
            var registerDto = AuthTestData.CreateRegisterUserDto();

            // Act
            var result = await service.RegisterUserAsync(registerDto);

            // Assert
            var data = Assert.IsType<Result<RegisteredUserDto>>(result);

            Assert.True(data.IsSuccess);

            var userManagerAfter = _fixture.GetUserManager();
            var finalCount = userManagerAfter.Users.Count();

            Assert.Equal(initialCount + 1, finalCount);

            var userInDb = await userManagerAfter.FindByNameAsync(registerDto.UserName);
            Assert.NotNull(userInDb);
            Assert.Equal(registerDto.Email, userInDb.Email);

            var claims = await userManagerAfter.GetClaimsAsync(userInDb);
            Assert.Contains(claims, c => c.Type == TS.Controller.Comment);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldLoginAdmin_WhenValidDataProvided()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedAdminAsync();

            var (service, _, _) = _fixture.GetScopedService<IAuthService>();
            var loginDto = AuthTestData.CreateAdminLoginDto();

            // Act
            var result = await service.AuthenticateAsync(loginDto);

            // Assert
            var data = Assert.IsType<Result<LoggedInUserDto>>(result);

            Assert.NotNull(data);
            Assert.Equal(ResultStatus.Success, data.Status);
            Assert.NotNull(data.Value!.Token);
            Assert.Equal(loginDto.UserName, data.Value!.UserName);
        }
    }
}
