using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PostApiService.Tests.Fixtures
{
    public class TestBaseFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        private string _connectionString;
        private bool _useDatabase;        

        public HttpClient Client { get; private set; }
        public IServiceProvider Services { get; private set; }

        public TestBaseFixture(string connectionString, bool useDatabase)
        {
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

                    ConfigureTestServices(services);
                });
            });

            Client = _factory.CreateClient();
            Services = _factory.Services;
        }

        protected virtual void ConfigureTestServices(IServiceCollection services) { }
        
        public async Task DisposeAsync()
        {
            _factory.Dispose();
            await Task.CompletedTask;
        }
    }
}
