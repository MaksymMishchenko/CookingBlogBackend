using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Contexts;
using PostApiService.Helper;
using PostApiService.Infrastructure.Authorization.Requirements;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.TypeSafe;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Security.Claims;
using System.Text;

namespace PostApiService.Infrastructure
{
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers application-specific services and the database context to the IServiceCollection.
        /// </summary>        
        public static IServiceCollection AddApplicationService(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddTransient<DbContext, ApplicationDbContext>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            services.AddTransient<IPostService, PostService>();
            services.AddTransient<ICommentService, CommentService>();
            services.AddTransient<ISnippetGeneratorService, SnippetGeneratorService>();

            return services;
        }

        /// <summary>
        /// Configures identity services for the application, including database context,
        /// identity management, and dependency injection for authentication and token services.
        /// </summary>        
        public static IServiceCollection AddAppIdentityService(this IServiceCollection services, string identityConnectionString)
        {
            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(identityConnectionString);
            });

            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddHttpContextAccessor();

            return services;
        }

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
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();
        }

        /// <summary>
        /// Configures JWT Bearer Authentication for the application.
        /// </summary>        
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

        public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {

                options.AddPolicy(TS.Policies.FullControlPolicy, policy =>
                {
                    policy.RequireRole(TS.Roles.Admin);
                });

                options.AddPolicy(TS.Policies.ContributorPolicy, policy =>
                {
                    policy.Requirements.Add(new ContributorRequirements());
                });
            });

            services.AddSingleton<IAuthorizationHandler, ContributorRequirementHandler>();

            return services;
        }

        public static async Task<IApplicationBuilder> SeedUserAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var cntx = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await cntx.Database.EnsureDeletedAsync();

                if (await cntx.Database.EnsureCreatedAsync())
                {
                    // Creating Role Entities
                    var adminRole = new IdentityRole(TS.Roles.Admin);
                    var contributorRole = new IdentityRole(TS.Roles.Contributor);

                    // Adding Roles
                    await roleManager.CreateAsync(adminRole);
                    await roleManager.CreateAsync(contributorRole);

                    // Creating User Entities
                    var adminUser = new IdentityUser() { UserName = "admin", Email = "admin@test.com" };
                    var contributorUser = new IdentityUser() { UserName = "cont", Email = "c@test.com" };

                    // Adding Users with Password
                    await userManager.CreateAsync(adminUser, "-Rtyuehe1");
                    await userManager.CreateAsync(contributorUser, "-Rtyuehe2");

                    // Adding Claims to Users
                    await userManager.AddClaimAsync(adminUser, new Claim(ClaimTypes.NameIdentifier, adminUser.Id));
                    await userManager.AddClaimAsync(adminUser, GetAdminClaims(TS.Controller.Post));

                    await userManager.AddClaimAsync(contributorUser, new Claim(ClaimTypes.NameIdentifier, contributorUser.Id));
                    await userManager.AddClaimAsync(contributorUser, GetContributorClaims(TS.Controller.Comment));

                    //// Adding Roles to Users
                    await userManager.AddToRoleAsync(adminUser, TS.Roles.Admin);
                    await userManager.AddToRoleAsync(contributorUser, TS.Roles.Contributor);
                }
            }
            return app;
        }

        private static Claim GetAdminClaims(string controllerName)
        {
            return new Claim(controllerName, ClaimHelper.SerializePermissions(
                TS.Permissions.Write,
                TS.Permissions.Update,
                TS.Permissions.Delete
                ));
        }

        private static Claim GetContributorClaims(string controllerName)
        {
            return new Claim(controllerName,
                ClaimHelper.SerializePermissions(
                    TS.Permissions.Write,
                    TS.Permissions.Update,
                    TS.Permissions.Delete
                ));
        }

        /// <summary>
        /// Seeds the database with initial data for posts and comments if no posts are already present.        
        /// </summary>        
        public static async Task<IApplicationBuilder> SeedDataAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var cntx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await cntx.Database.EnsureDeletedAsync();


                if (await cntx.Database.EnsureCreatedAsync())
                {
                    var postsList = SeedData.GetPostsWithComments(count: 150, commentCount: 20);

                    await cntx.Posts.AddRangeAsync(postsList);

                    var allComments = postsList
                        .SelectMany(p => p.Comments)
                        .ToList();

                    if (allComments.Any())
                    {
                        await cntx.Comments.AddRangeAsync(allComments);
                    }

                    await cntx.SaveChangesAsync();
                }
            }
            return app;
        }

        /// <summary>
        /// Configures CORS (Cross-Origin Resource Sharing) to allow requests from specific origins.       
        /// </summary>       
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
