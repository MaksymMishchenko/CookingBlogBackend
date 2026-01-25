using Microsoft.Extensions.DependencyInjection;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Interfaces;
using PostApiService.Models.TypeSafe;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class AuthServiceIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _provider;

        public AuthServiceIntegrationTests()
        {
            var services = new ServiceCollection();

            var dbName = Guid.NewGuid().ToString();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.Configure<JwtConfiguration>(options =>
            {
                options.SecretKey = "very-long-and-super-secret-key-for-testing";
                options.Issuer = "test";
                options.Audience = "test";
                options.TokenExpirationMinutes = 60;
            });

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddLogging();

            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldRegisterUser_AndAssignClaims()
        {
            // Arrange
            var authService = _provider.GetRequiredService<IAuthService>();
            var registerDto = AuthTestData.CreateRegisterUserDto();

            // Act
            var result = await authService.RegisterUserAsync(registerDto);

            // Assert
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);

            var authRepo = _provider.GetRequiredService<IAuthRepository>();
            var userInDb = await authRepo.FindByNameAsync(registerDto.UserName);

            Assert.NotNull(userInDb);
            Assert.Equal(registerDto.Email, userInDb.Email);

            var claims = await authRepo.GetClaimsAsync(userInDb);
            Assert.Contains(claims, c => c.Type == TS.Controller.Comment);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldLoginUser_WhenValidDataProvided()
        {
            // Arrange                        
            var loginDto = AuthTestData.CreateUserLoginDto();
            var identityUser = AuthTestData.CreateIdentityUser();

            var authRepo = _provider.GetRequiredService<IAuthRepository>();
            await authRepo.CreateAsync(identityUser, loginDto.Password);

            var authService = _provider.GetRequiredService<IAuthService>();

            // Act
            var result = await authService.AuthenticateAsync(loginDto);

            // Assert
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result);
            Assert.NotNull(result.Value!.Token);
            Assert.Equal(loginDto.UserName, result.Value!.UserName);
        }
        
        public void Dispose()
        {
            _provider.Dispose();
        }
    }
}
