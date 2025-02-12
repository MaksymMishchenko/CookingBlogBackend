using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PostApiService.Tests.Fixtures
{
    public class PostControllerFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        private const string _connectionString = "Server=MAX\\SQLEXPRESS;Database=TestPost;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";
        public HttpClient Client { get; private set; }
        public IServiceProvider Services { get; private set; }

        public PostControllerFixture()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlServer(_connectionString);
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
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();

                await dbContext.Posts.AddRangeAsync(TestDataHelper.GetPostsWithComments());
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task DisposeAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
            }
        }
    }
}
