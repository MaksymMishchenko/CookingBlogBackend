using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PostApiService.Contexts;
using PostApiService.Helper;
using PostApiService.Models.TypeSafe;
using System.Security.Claims;

namespace PostApiService.Tests.Fixtures
{
    public class BaseTestFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program>? _factory;
        private readonly string _identityConnectionString;
        private readonly string _connectionString;
        private readonly bool _useDatabase;

        public HttpClient? Client { get; private set; }
        public IServiceProvider? Services { get; private set; }

        public BaseTestFixture(string connectionString,
            string identityConnectionString,
            bool useDatabase)
        {
            _identityConnectionString = identityConnectionString;
            _connectionString = connectionString;
            _useDatabase = useDatabase;
        }

        public virtual async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureTestServices(services =>
                {
                    if (_useDatabase)
                    {
                        services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                        services.AddDbContext<ApplicationDbContext>(options =>
                        {
                            options.UseSqlServer(_connectionString);
                        });
                    }

                    services.RemoveAll(typeof(DbContextOptions<AppIdentityDbContext>));
                    services.AddDbContext<AppIdentityDbContext>(options =>
                    {
                        options.UseSqlServer(_identityConnectionString);
                    });

                    ConfigureTestServices(services);
                });
            });

            Client = _factory.CreateClient();
            Services = _factory.Services;

            await InitializeTestUsersDatabaseAsync();
        }

        protected virtual void ConfigureTestServices(IServiceCollection services) { }

        protected virtual async Task InitializeTestUsersDatabaseAsync()
        {
            using (var scope = Services?.CreateScope())
            {
                var cntx = scope!.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (await cntx.Database.EnsureCreatedAsync())
                {
                    // Creating Role Entities
                    var adminRole = new IdentityRole(TS.Roles.Admin);
                    var contributorRole = new IdentityRole(TS.Roles.Contributor);

                    // Adding Roles
                    await roleManager.CreateAsync(adminRole);
                    await roleManager.CreateAsync(contributorRole);

                    // Creating User Entities
                    var adminUser = new IdentityUser() { Id = "testAdminId", UserName = "admin", Email = "admin@test.com" };
                    var contributorUser = new IdentityUser() { Id = "testContId", UserName = "cont", Email = "c@test.com" };

                    // Adding Users with Password
                    await userManager.CreateAsync(adminUser, "-Rtyuehe1");
                    await userManager.CreateAsync(contributorUser, "-Rtyuehe2");

                    // Adding Claims to Users
                    await userManager.AddClaimAsync(adminUser, new Claim(ClaimTypes.NameIdentifier, "testAdminId"));

                    await userManager.AddClaimAsync(contributorUser, new Claim(ClaimTypes.NameIdentifier, "testContId"));
                    await userManager.AddClaimAsync(contributorUser, GetContributorClaims(TS.Controller.Comment));

                    // Adding Roles to Users
                    await userManager.AddToRoleAsync(adminUser, TS.Roles.Admin);
                    await userManager.AddToRoleAsync(contributorUser, TS.Roles.Contributor);
                }
            }
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

        public virtual async Task DisposeAsync()
        {
            _factory?.Dispose();
            await Task.CompletedTask;
        }
    }
}
