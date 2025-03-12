using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Contexts;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;
using System.Text;

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

            services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

            services.AddTransient<IPostService, PostService>();
            services.AddTransient<ICommentService, CommentService>();

            return services;
        }

        /// <summary>
        /// Seeds the database with initial data for posts and comments if no posts are already present.
        /// This method ensures that the database is created and populated with sample data during application startup.
        /// </summary>
        /// <param name="app">The WebApplication instance to configure services and perform seeding.</param>
        /// <returns>The WebApplication instance for method chaining.</returns>
        public static async Task<IApplicationBuilder> SeedDataAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var cntx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await cntx.Database.EnsureDeletedAsync();

                if (await cntx.Database.EnsureCreatedAsync())
                {
                    var posts = new List<Post>
                    {
                        new Post
                        {
                            Title = "First Post",
                            Description = "Description for first post",
                            Content = "This is the content of the first post.",
                            Author = "Peter Jack",
                            CreateAt = DateTime.Now,
                            ImageUrl = "/images/placeholder.jpg",
                            MetaTitle = "Meta title info",
                            MetaDescription = "This is meta description",
                            Slug = "http://localhost:4200/first-post"
                        },
                        new Post
                        {
                            Title = "Second Post",
                            Description = "Description for second post",
                            Content = "This is the content of the second post.",
                            Author = "Jay Way",
                            CreateAt = DateTime.Now,
                            ImageUrl = "/images/placeholder.jpg",
                            MetaTitle = "Meta title info 2",
                            MetaDescription = "This is meta description 2",
                            Slug = "http://localhost:4200/second-post"
                        }
                    };

                    await cntx.Posts.AddRangeAsync(posts);
                    await cntx.SaveChangesAsync();

                    var comments = new List<Comment>
                    {
                        new Comment
                        {
                            Author = "John Doe",
                            Content = "Great post!",
                            CreatedAt = DateTime.Now,
                            PostId = posts[0].PostId,
                            UserId = "testUserId"
                        },
                        new Comment
                        {
                            Author = "Jane Doe",
                            Content = "I totally agree with this!",
                            CreatedAt = DateTime.Now,
                            PostId = posts[0].PostId,
                            UserId = "testUserId"
                        },
                        new Comment
                        {
                            Author = "Alice",
                            Content = "This is a comment on the second post.",
                            CreatedAt = DateTime.Now,
                            PostId = posts[1].PostId,
                            UserId = "testUserId"
                        }
                    };
                    await cntx.Comments.AddRangeAsync(comments);
                    await cntx.SaveChangesAsync();
                }
            }
            return app;
        }

        /// <summary>
        /// Configures identity services for the application, including database context,
        /// identity management, and dependency injection for authentication and token services.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to.</param>
        /// <param name="identityConnectionString">The connection string for the Identity database.</param>
        /// <returns>The IServiceCollection with the configured identity services.</returns>
        public static IServiceCollection AddAppIdentityService(this IServiceCollection services, string identityConnectionString)
        {
            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(identityConnectionString);
            });

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
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

        /// <summary>
        /// Configures CORS (Cross-Origin Resource Sharing) to allow requests from specific origins.
        /// In this case, it allows requests from "http://localhost:4200".
        /// This is useful to enable cross-origin requests between the frontend (Angular) and backend (API).
        /// </summary>
        /// <param name="services">The IServiceCollection to register the CORS policy.</param>
        /// <returns>The updated IServiceCollection with the CORS policy registered.</returns>
        public static IServiceCollection AddAppCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}
