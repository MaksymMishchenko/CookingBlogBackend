using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PostApiService.Tests.Fixtures
{
    public class PostFixture : BaseTestFixture
    {
        private const string _connectionString = "Server=MAX\\SQLEXPRESS;Database=TestPost;Trusted_Connection=True;" +
            "MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public PostFixture() : base(_connectionString, useDatabase: true) { }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.RemoveAll(typeof(IAuthenticationHandler));
            services.RemoveAll(typeof(AuthenticationSchemeOptions));

            services.AddAuthentication("DynamicScheme")
                    .AddScheme<AuthenticationSchemeOptions, DynamicAuthHandler>(
                        "DynamicScheme", options => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "DynamicScheme";
                options.DefaultChallengeScheme = "DynamicScheme";
            });
        }

        public void SetCurrentUser(ClaimsPrincipal user)
        {
            DynamicAuthHandler.CurrentPrincipal = user;
        }

        public class DynamicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public static ClaimsPrincipal CurrentPrincipal { get; set; }

            public DynamicAuthHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                if (CurrentPrincipal == null)
                {
                    return Task.FromResult(AuthenticateResult.Fail("No principal set."));
                }

                var ticket = new AuthenticationTicket(CurrentPrincipal, "DynamicScheme");
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }
    }
}
