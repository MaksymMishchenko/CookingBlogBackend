using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PostApiService.Tests.Fixtures
{
    public class PostControllerFixture
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
    }
}
