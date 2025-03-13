﻿using Microsoft.AspNetCore.Identity;
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

            var dbName = Guid.NewGuid().ToString();

            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();

            services.AddLogging();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

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

        [Fact]
        public async Task LoginUserAsync_ShouldLoginUser_WhenValidDataProvided()
        {
            // Arrange            
            var testUser = new IdentityUser { UserName = "testUser", Email = "validemail@test.com" };
            var password = "Password123!";

            var userManager = _provider.GetRequiredService<UserManager<IdentityUser>>();
            await userManager.CreateAsync(testUser, password);

            var user = new LoginUser
            {
                UserName = "testUser",
                Password = "Password123!"
            };

            var authService = _provider.GetRequiredService<IAuthService>();

            // Act
            var authenticatedUser = await authService.LoginAsync(user);

            // Assert
            Assert.NotNull(authenticatedUser);
            Assert.Equal(user.UserName, authenticatedUser.UserName);
        }
    }
}
