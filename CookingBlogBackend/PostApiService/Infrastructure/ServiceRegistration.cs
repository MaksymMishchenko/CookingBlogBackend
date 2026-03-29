using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Helper;
using PostApiService.Infrastructure.Authorization.Requirements;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Infrastructure.Constants;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.TypeSafe;
using PostApiService.Repositories;
using PostApiService.Services;
using Serilog.Exceptions;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace PostApiService.Infrastructure
{
    public static class ServiceRegistration
    {
        /// <summary>
        /// Configures Serilog for file and console logging.
        /// </summary>        
        public static void AddAppLogging(this IHostBuilder host, IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithThreadId()
                .Enrich.WithMachineName()
                .CreateLogger();

            host.UseSerilog();
        }

        /// <summary>
        /// Registers application-specific services and the database context to the IServiceCollection.
        /// </summary>        
        public static IServiceCollection AddApplicationService(this IServiceCollection services,
            IConfiguration configuration, string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();

            services.AddScoped<IWebContext, WebContext>();
            services.AddHttpContextAccessor();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddTransient<ISnippetGeneratorService, SnippetGeneratorService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICommentService, CommentService>();

            services.AddExternalTools(configuration);
            services.AddAppRateLimiting(configuration);

            return services;
        }

        public static IServiceCollection AddExternalTools(this IServiceCollection services, IConfiguration configuration)
        {
            // Get sanitizer configuration from appsettings.json
            var section = configuration.GetSection(ConfigConstants.HtmlSanitizer);

            if (!section.Exists())
            {
                throw new InvalidOperationException(string.Format(
                    Errors.ConfigSectionMissing, ConfigConstants.HtmlSanitizer));
            }

            services.Configure<SanitizerConfiguration>(section);
            services.AddSingleton<IHtmlSanitizationService, HtmlSanitizationService>();

            return services;
        }

        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RateLimitOptions>()
                .Bind(configuration.GetSection(RateLimitSection))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(RateLimitOptions.PolicyName, httpContext =>
                {
                    var rateOptions = httpContext.RequestServices
                        .GetRequiredService<IOptions<RateLimitOptions>>().Value;

                    if (httpContext.User.IsInRole(TS.Roles.Admin))
                    {
                        return RateLimitPartition.GetNoLimiter(TS.Roles.Admin);
                    }

                    var userKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                  ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: userKey,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateOptions.PermitLimit,
                            Window = TimeSpan.FromMinutes(rateOptions.WindowMinutes),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                options.OnRejected = async (context, ct) =>
                {
                    var rateOptions = context.HttpContext.RequestServices
                        .GetRequiredService<IOptions<RateLimitOptions>>().Value;

                    Log.Warning(Security.RateLimitExceeded,
                        context.HttpContext.Connection.RemoteIpAddress,
                        context.HttpContext.Request.Path,
                        context.HttpContext.Request.Method);

                    var message = string.Format(
                        RateLimitOptions.Errors.LimitExceeded,
                        rateOptions.PermitLimit,
                        rateOptions.WindowMinutes
                    );

                    var errorResponse = ApiResponse.CreateErrorResponse(
                        message: message,
                        errorCode: RateLimitOptions.Errors.ErrorCode
                    );

                    context.HttpContext.Response.ContentType = "application/json";

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
                };
            });

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
                var provider = scope.ServiceProvider;
                var cntx = provider.GetRequiredService<ApplicationDbContext>();
                var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
                var env = provider.GetRequiredService<IWebHostEnvironment>();
                var config = provider.GetRequiredService<IConfiguration>();
                
                if (env.IsEnvironment("Testing")) return app;
                
                if (env.IsProduction() || env.IsStaging())
                {
                    Log.Information("--- {Env}: Applying Migrations ---", env.EnvironmentName);
                    await cntx.Database.MigrateAsync();
                }
                else
                {
                    Log.Information("--- Development: Recreating Database ---");
                    await cntx.Database.EnsureDeletedAsync();
                    await cntx.Database.EnsureCreatedAsync();
                }
                
                if (!await userManager.Users.AnyAsync())
                {
                    Log.Information("--- Seeding Roles ---");
                    await roleManager.CreateAsync(new IdentityRole(TS.Roles.Admin));
                    await roleManager.CreateAsync(new IdentityRole(TS.Roles.Contributor));
                    
                    var adminEmail = config["SeedSettings:AdminEmail"];
                    var adminPass = config["SeedSettings:AdminPassword"];
                    var adminName = config["SeedSettings:AdminUserName"];

                    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPass))
                    {
                        var adminUser = new IdentityUser { UserName = adminName, Email = adminEmail };
                        var adminResult = await userManager.CreateAsync(adminUser, adminPass);

                        if (adminResult.Succeeded)
                        {                           
                            await userManager.AddClaimAsync(adminUser, new Claim(ClaimTypes.NameIdentifier, adminUser.Id));
                            await userManager.AddClaimAsync(adminUser, GetAdminClaims(TS.Controller.Post));
                            await userManager.AddClaimAsync(adminUser, GetAdminClaims(TS.Controller.Comment));

                            await userManager.AddToRoleAsync(adminUser, TS.Roles.Admin);
                            Log.Information("--- Admin [{Email}] created successfully ---", adminEmail);
                        }
                        else
                        {
                            Log.Error("--- Failed to create Admin: {Errors} ---", string.Join(", ", adminResult.Errors.Select(e => e.Description)));
                        }
                    }
                   
                    if (!env.IsProduction())
                    {
                        var contEmail = config["SeedSettings:ContEmail"];
                        var contPass = config["SeedSettings:ContPassword"];
                        var contName = config["SeedSettings:ContUserName"];

                        if (!string.IsNullOrEmpty(contEmail) && !string.IsNullOrEmpty(contPass))
                        {
                            var contributorUser = new IdentityUser { UserName = contName, Email = contEmail };
                            var contResult = await userManager.CreateAsync(contributorUser, contPass);

                            if (contResult.Succeeded)
                            {                                
                                await userManager.AddClaimAsync(contributorUser, new Claim(ClaimTypes.NameIdentifier, contributorUser.Id));
                                await userManager.AddClaimAsync(contributorUser, GetContributorClaims(TS.Controller.Comment));

                                await userManager.AddToRoleAsync(contributorUser, TS.Roles.Contributor);
                                Log.Information("--- Contributor [{Email}] created successfully ---", contEmail);
                            }
                        }
                    }
                }
                else
                {
                    Log.Information("--- Database already seeded. Skipping ---");
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

                if (await cntx.Posts.AnyAsync())
                {
                    cntx.Posts.RemoveRange(cntx.Posts);
                    await cntx.SaveChangesAsync();

                    if (await cntx.Categories.AnyAsync())
                    {
                        cntx.Categories.RemoveRange(cntx.Categories);
                        await cntx.SaveChangesAsync();
                    }
                }

                var postsList = SeedData.GetPostsWithComments
                    (count: 150, commentCount: 10, userIds: userIds);

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
