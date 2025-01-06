using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PostApiService.Tests.Fixtures
{
    public class WebApplicationFactoryFixture : IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; }
        public int InitializePostData { get; set; } = 3;

        private WebApplicationFactory<Program> _factory;


        private readonly string _connectionString =
            $"Server=(localdb)\\ProjectModels;Database=IntegrationTest;Trusted_Connection=true;MultipleActiveResultSets=true";
        public WebApplicationFactoryFixture()
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

                    services.RemoveAll(typeof(IAuthenticationService));

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestBearer";
                        options.DefaultChallengeScheme = "TestBearer";
                    }).AddJwtBearer("TestBearer", options =>
                    {                        
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes("kYM3sxL49j4dz7AX7Sh5Np9yNX7Tb6v7kYM3sxL49j4dz7AX7Sh5Np9yNX7Tb6v7"))
                        };
                    });
                });
            });
            HttpClient = _factory.CreateClient();
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var scopedService = scope.ServiceProvider;
                var cntx = scopedService.GetRequiredService<ApplicationDbContext>();
                await cntx.Database.EnsureCreatedAsync();
                await cntx.Posts.AddRangeAsync(DataFixture.GetPosts(InitializePostData));
                await cntx.SaveChangesAsync();
            }
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var scopedService = scope.ServiceProvider;
                var cntx = scopedService.GetRequiredService<ApplicationDbContext>();
                await cntx.Database.EnsureDeletedAsync();
            }
        }
    }
}
