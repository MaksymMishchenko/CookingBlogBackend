using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PostApiService.Contexts;

namespace PostApiService.Tests.Fixtures
{
    public class AuthFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        private const string _identityConnectionString = "Server=MAX\\SQLEXPRESS;Database=IdentityTestDb;" +
           "Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public HttpClient Client { get; private set; }
        public IServiceProvider Services { get; private set; }

        public virtual async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<AppIdentityDbContext>));
                    services.AddDbContext<AppIdentityDbContext>(options =>
                    {
                        options.UseSqlServer(_identityConnectionString);
                    });
                });
            });

            Client = _factory.CreateClient();
            Services = _factory.Services;
        }

        public async Task DisposeAsync()
        {
            using (var scope = Services.CreateScope())
            {
                var cntx = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await cntx.Database.EnsureCreatedAsync();
                await cntx.Database.EnsureCreatedAsync();
            }

            _factory.Dispose();
            await Task.CompletedTask;
        }
    }
}
