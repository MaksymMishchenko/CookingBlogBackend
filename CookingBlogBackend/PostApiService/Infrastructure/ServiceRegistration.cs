using Microsoft.EntityFrameworkCore;
using PostApiService.Contexts;
using PostApiService.Interfaces;
using PostApiService.Services;

namespace PostApiService.Infrastructure
{
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers application-specific services and the database context to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to which services will be added.</param>
        /// <param name="connectionString">The connection string used to configure the database context.</param>
        /// <returns>The IServiceCollection with the added services.</returns>
        public static IServiceCollection AddApplicationService(this IServiceCollection services, string connectionString)
        {            
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddTransient<IPostService, PostService>();
            services.AddTransient<ICommentService, CommentService>();

            return services;
        }

        /// <summary>
        /// Registers application-specific services and the identity database context to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to which services will be added.</param>
        /// <param name="identityConnectionString">The connection string used to configure the identity database context.</param>
        /// <returns>The IServiceCollection with the added services.</returns>
        public static IServiceCollection AddAppIdentityService(this IServiceCollection services, string identityConnectionString)
        {
            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(identityConnectionString);
            });

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
