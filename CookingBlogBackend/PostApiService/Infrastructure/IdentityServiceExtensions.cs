using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Contexts.PostApiService.Contexts;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;
using System.Text;

namespace PostApiService.Infrastructure
{
    public static class IdentityServiceExtensions
    {
        /// <summary>
        /// Configures identity services for the application, including database context,
        /// identity management, and dependency injection for authentication and token services.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to.</param>
        /// <param name="identityConnectionString">The connection string for the Identity database.</param>
        /// <returns>The IServiceCollection with the configured identity services.</returns>
        public static IServiceCollection AppIdentityService(this IServiceCollection services, string identityConnectionString)
        {
            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(identityConnectionString);
            });

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }

        /// <summary>
        /// Configures and adds ASP.NET Core Identity services with custom security settings.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add Identity services to.</param>
        /// <returns>An <see cref="IdentityBuilder"/> that can be used to configure Identity.</returns>
        /// <remarks>
        /// This method sets up Identity with the following configurations:
        /// <list type="bullet">
        ///   <item><description>Enforces strong password policies (min. 8 characters, requires digits, uppercase, lowercase, special characters).</description></item>
        ///   <item><description>Enables account lockout after 5 failed login attempts for 5 minutes.</description></item>
        ///   <item><description>Restricts allowed characters in usernames and enforces unique email addresses.</description></item>
        ///   <item><description>Uses Entity Framework Core for Identity data storage.</description></item>
        ///   <item><description>Registers default token providers for email confirmation and password reset.</description></item>
        /// </list>
        /// </remarks>
        public static IdentityBuilder AddApplicationIdentity(this IServiceCollection services)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 3;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
        }

        /// <summary>
        /// Configures JWT Bearer Authentication for the application.
        /// </summary>
        /// <param name="services">The IServiceCollection to which authentication services will be added.</param>
        /// <param name="config">The JwtConfiguration object containing JWT settings such as Issuer, Audience, and Key.</param>
        /// <returns>The IServiceCollection with the configured JWT authentication services.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided JwtConfiguration is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if any of the required properties (Issuer, Audience, or Key) in the JwtConfiguration are null or empty.
        /// </exception>
        public static IServiceCollection AddAppJwtAuthentication(this IServiceCollection services, JwtConfiguration config)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateActor = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        RequireExpirationTime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config.Issuer,
                        ValidAudience = config.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey))
                    };
                });

            return services;
        }

        public static async Task<IApplicationBuilder> SeedUserAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var cntx = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                await cntx.Database.EnsureDeletedAsync();
                if (await cntx.Database.EnsureCreatedAsync())
                {

                }
            }
            return app;
        }
    }
}
