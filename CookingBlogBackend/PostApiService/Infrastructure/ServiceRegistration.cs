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
using System.Data.Common;
using System.Net.Sockets;
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
                    policy.Requirements.Add(new PermissionRequirement(TS.Controller.Post));
                });
                
                options.AddPolicy(TS.Policies.ContributorPolicy, policy =>
                {
                    policy.Requirements.Add(new PermissionRequirement(TS.Controller.Comment));
                });
            });

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            return services;
        }

        public static async Task<IApplicationBuilder> SeedUsersAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var cntx = provider.GetRequiredService<ApplicationDbContext>();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var config = provider.GetRequiredService<IConfiguration>();

            if (env.IsEnvironment("Testing")) return app;

            if (env.IsDevelopment())
            {
                await ExecuteWithRetryAsync(async () =>
                {
                    Log.Warning("--- Development Mode: Dropping and Recreating Database ---");
                    await cntx.Database.EnsureDeletedAsync();
                    await cntx.Database.MigrateAsync();
                }, "Database Reset");
            }
            else
            {
                await ExecuteWithRetryAsync(async () =>
                    await cntx.Database.MigrateAsync(), $"Migration ({env.EnvironmentName})");
            }

            await ExecuteWithRetryAsync(async () =>
            {
                Log.Information("--- Starting Identity Seeding ---");

                await EnsureRolesAsync(roleManager);
                await SeedAdminAsync(userManager, config);

                if (!env.IsProduction())
                {
                    await SeedContributorAsync(userManager, config);
                }

            }, "Identity Seeding");

            return app;
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { TS.Roles.Admin, TS.Roles.Contributor };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminAsync(UserManager<IdentityUser> userManager, IConfiguration config)
        {
            var email = config["SeedSettings:AdminEmail"];
            var pass = config["SeedSettings:AdminPassword"];
            var name = config["SeedSettings:AdminUserName"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                Log.Warning("--- SeedAdmin: Missing credentials in Configuration! ---");
                return;
            }

            var existingAdmin = await userManager.FindByEmailAsync(email);
            if (existingAdmin != null)
            {
                Log.Information("--- Admin [{Email}] already exists. Skipping ---", email);
                return;
            }

            var user = new IdentityUser { UserName = name, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, pass);

            if (result.Succeeded)
            {
                await userManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id));
                await userManager.AddClaimAsync(user, GetAdminClaims(TS.Controller.Post));
                await userManager.AddClaimAsync(user, GetAdminClaims(TS.Controller.Comment));
                await userManager.AddToRoleAsync(user, TS.Roles.Admin);
                Log.Information("--- Admin [{Email}] created successfully ---", email);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Log.Error("--- Admin creation FAILED: {Errors} ---", errors);
            }
        }

        private static async Task SeedContributorAsync(UserManager<IdentityUser> userManager, IConfiguration config)
        {
            var email = config["SeedSettings:ContEmail"];
            var pass = config["SeedSettings:ContPassword"];
            var name = config["SeedSettings:ContUserName"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                Log.Warning("--- SeedContributor: Missing credentials in Configuration! ---");
                return;
            }

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                Log.Information("--- Contributor [{Email}] already exists. Skipping ---", email);
                return;
            }

            var user = new IdentityUser { UserName = name, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, pass);

            if (result.Succeeded)
            {
                await userManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id));
                await userManager.AddClaimAsync(user, GetContributorClaims(TS.Controller.Comment));
                await userManager.AddToRoleAsync(user, TS.Roles.Contributor);
                Log.Information("--- Contributor [{Email}] created successfully ---", email);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Log.Error("--- Contributor creation FAILED for [{Email}]: {Errors} ---", email, errors);
            }
        }

        private static Claim GetAdminClaims(string controllerName)
        {
            return new Claim(controllerName, ClaimHelper.SerializePermissions(
                TS.Permissions.Write,
                TS.Permissions.Update,
                TS.Permissions.Delete
                )
            );
        }

        private static Claim GetContributorClaims(string controllerName)
        {
            return new Claim(controllerName,
                ClaimHelper.SerializePermissions(
                    TS.Permissions.Write,
                    TS.Permissions.Update,
                    TS.Permissions.Delete
                )
            );
        }

        public static async Task<IApplicationBuilder> SeedDataAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var cntx = provider.GetRequiredService<ApplicationDbContext>();
                var env = provider.GetRequiredService<IWebHostEnvironment>();

                if (env.IsProduction() || env.IsEnvironment("Testing")) return app;

                await ExecuteWithRetryAsync(async () =>
                {
                    if (await cntx.Posts.AnyAsync())
                    {
                        Log.Information("--- SeedData: Posts already exist. Skipping ---");
                        return;
                    }

                    var userIds = await cntx.Users.Select(u => u.Id).ToArrayAsync();

                    if (userIds.Length == 0)
                    {
                        Log.Warning("--- SeedData: No users found. Identity seed might have failed! ---");
                        return;
                    }

                    Log.Information("--- SeedData: Generating 150 posts with comments... ---");

                    var postsList = SeedData.GetPostsWithComments(
                        count: 150,
                        commentCount: 10,
                        userIds: userIds);

                    await cntx.Posts.AddRangeAsync(postsList);
                    await cntx.SaveChangesAsync();

                    Log.Information("--- SeedData: 150 posts with comments added successfully ---");

                }, "Fake Data Seeding");
            }
            return app;
        }

        private static async Task ExecuteWithRetryAsync(
            Func<Task> action,
            string taskName,
            int maxRetries = 5,
            int initialDelayMs = 2000)
        {
            for (int i = 1; i <= maxRetries; i++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex) when (ex is DbException ||
                    ex is SocketException ||
                    ex.InnerException is SocketException)
                {
                    if (i == maxRetries)
                    {
                        Log.Fatal(ex, "--- {TaskName} FATAL: Database unreachable after {Max} attempts ---", taskName, maxRetries);
                        throw;
                    }

                    int currentDelay = initialDelayMs * (int)Math.Pow(2, i - 1);

                    Log.Warning("--- {TaskName} failed (Connection issue). Attempt {Attempt}/{Max}. Retrying in {Delay}ms... ---",
                        taskName, i, maxRetries, currentDelay);

                    await Task.Delay(currentDelay);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "--- {TaskName} failed due to a LOGIC error (No Retry) ---", taskName);
                    throw;
                }
            }
        }

        /// <summary>
        /// Configures CORS (Cross-Origin Resource Sharing) to allow requests from specific origins.       
        /// </summary>       
        public static IServiceCollection AddAppCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", builder =>
                {
                    builder.WithOrigins(
                            "https://cooking-blog-frontend-staging.onrender.com",
                            "https://cooking-blog-frontend-production.onrender.com",
                            "http://localhost:4200"
                    )
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}
