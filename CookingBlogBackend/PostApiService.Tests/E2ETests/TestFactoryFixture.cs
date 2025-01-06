using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PostApiService.Tests.E2ETests
{
    public class TestFactoryFixture : IAsyncLifetime
    {
        public WebApplicationFactory<Program>? Factory { get; private set; }
        public HttpClient Client => Factory!.CreateClient();

        public async Task InitializeAsync()
        {
            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    var appDbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (appDbContextDescriptor != null)
                    {
                        services.Remove(appDbContextDescriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {           
            Factory!.Dispose();
            await Task.CompletedTask;
        }
    }
}
