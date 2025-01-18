using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PostApiService.Tests.Fixtures
{
    public class WebApplicationFactoryFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;

        private const string _databaseName = "Server=MAX\\SQLEXPRESS;Database=TestPostApiDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";
        public HttpClient Client { get; private set; }
        public IServiceProvider Services { get; }

        public WebApplicationFactoryFixture()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlServer(_databaseName);
                    });
                });
            });

            Client = _factory.CreateClient();
            Services = _factory.Services;
        }

        public async Task InitializeAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var scopeService = scope.ServiceProvider;
                var cntx = scopeService.GetRequiredService<ApplicationDbContext>();

                await cntx.Database.EnsureDeletedAsync();
                await cntx.Database.EnsureCreatedAsync();
                await cntx.Posts.AddRangeAsync(TestDataHelper.GetPostWithComments(commentCount: 3));
                await cntx.SaveChangesAsync();
            }
        }

        public async Task DisposeAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var scopeService = scope.ServiceProvider;
                var cntx = scopeService.GetRequiredService<ApplicationDbContext>();

                await cntx.Database.EnsureDeletedAsync();
            }
        }
    }
}
