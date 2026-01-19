using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Tests.Infrastructure;
using Respawn;

namespace PostApiService.Tests.Fixtures
{
    public class BaseTestFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program>? _factory;

        private Respawner _respawner = default!;
        private SqlConnection _connection = default!;

        public HttpClient? Client { get; private set; }
        public IServiceProvider? Services { get; private set; }

        public virtual async Task InitializeAsync()
        {
            await SharedDbContainer.StartAsync();

            _connection = new SqlConnection(SharedDbContainer.ConnectionString);
            await _connection.OpenAsync();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureTestConfig();
                builder.ConfigureTestServices(services =>
                {
                    services.AddTestAuth();
                    services.AddTestDatabase(SharedDbContainer.ConnectionString);
                    ConfigureTestServices(services);
                });
            });

            Client = _factory.CreateClient();
            Services = _factory.Services;

            await EnsureDatabaseCreatedAsync();
            await InitializeRespawnerAsync();
            await Services!.SeedDefaultUsersAsync();
        }

        private async Task EnsureDatabaseCreatedAsync()
        {
            using var scope = Services!.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        private async Task InitializeRespawnerAsync()
        {
            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                SchemasToInclude = ["dbo"],
                WithReseed = true,
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
        }

        protected virtual void ConfigureTestServices(IServiceCollection services) { }

        public async Task ResetDatabaseAsync()
        {
            await _respawner.ResetAsync(_connection);
        }

        public void LoginAsAdmin()
        {
            Client!.DefaultRequestHeaders.Remove(TestUserData.TestUserHeader);
            Client!.DefaultRequestHeaders.Add(TestUserData.TestUserHeader, TestUserData.AdminKey);
        }

        public void LoginAsContributor()
        {
            Client!.DefaultRequestHeaders.Remove(TestUserData.TestUserHeader);
            Client!.DefaultRequestHeaders.Add(TestUserData.TestUserHeader, TestUserData.ContributorKey);
        }

        public virtual async Task DisposeAsync()
        {
            _factory?.Dispose();

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}
