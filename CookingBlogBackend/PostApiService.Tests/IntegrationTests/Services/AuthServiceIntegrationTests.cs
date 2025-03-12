using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Contexts;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class AuthServiceIntegrationTests
    {
        private readonly ServiceProvider _provider;

        public AuthServiceIntegrationTests()
        {
            var services = new ServiceCollection();
            
            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseInMemoryDatabase("IdentityTestDb"));
            
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();
           
            services.AddLogging();
            
            services.AddTransient<IAuthService, AuthService>();            
            
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldRegisterUser_WhenValidDataProvided()
        {
            // Arrange
            var authService = _provider.GetRequiredService<IAuthService>();

            var user = new RegisterUser
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            await authService.RegisterUserAsync(user);

            // Assert
            var userManager = _provider.GetRequiredService<UserManager<IdentityUser>>();
            var createdUser = await userManager.FindByNameAsync(user.UserName);

            Assert.NotNull(createdUser);
            Assert.Equal(user.Email, createdUser.Email);
        }
    }
}
