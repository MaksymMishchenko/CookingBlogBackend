using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Helper;
using PostApiService.Infrastructure.Authorization.Requirements;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
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
        /// Configures Serilog for file and console logging.
        /// </summary>
        public static void AddAppLogging(this IHostBuilder host)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/api_validation_.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            host.UseSerilog();
        }

        /// <summary>
        /// Registers application-specific services and the database context to the IServiceCollection.
        /// </summary>        
        public static IServiceCollection AddApplicationService(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();

            services.AddScoped<IWebContext, WebContext>();
            services.AddHttpContextAccessor();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddTransient<ISnippetGeneratorService, SnippetGeneratorService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICommentService, CommentService>();

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
                .AddEntityFrameworkStores<ApplicationDbContext>()
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
                var cntx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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

                if (await cntx.Posts.AnyAsync()) return app;

                var userIds = await cntx.Users
                    .Select(u => u.Id)
                    .ToArrayAsync();

                if (userIds.Length == 0)
                {
                    throw new Exception("SeedData: No users found in database. Seed users first!");
                }

                var postsList = SeedData.GetPostsWithComments
                    (count: 150, commentCount: 20, userIds: userIds);

                await cntx.Posts.AddRangeAsync(postsList);
                await cntx.SaveChangesAsync();
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
