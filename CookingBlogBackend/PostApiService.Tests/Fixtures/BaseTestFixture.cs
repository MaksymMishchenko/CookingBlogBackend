using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PostApiService.Tests.Fixtures
{
    public class TestBaseFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        private readonly string _connectionString;
        private readonly bool _useDatabase;

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

                    services.AddAuthentication("TestScheme")
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                                "TestScheme", options =>
                                {
                                    // Optional: set custom time provider for testing
                                    // options.TimeProvider = TimeProvider.System;
                                });

                    services.PostConfigure<AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestScheme";
                        options.DefaultChallengeScheme = "TestScheme";
                    });

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

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
