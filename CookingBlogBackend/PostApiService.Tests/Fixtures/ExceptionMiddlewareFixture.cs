using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostApiService.Contexts;
using PostApiService.Interfaces;
using PostApiService.Tests.Mocks;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PostApiService.Tests.Fixtures
{
    public class ExceptionMiddlewareFixture : BaseTestFixture
    {
        private const string _identityConnectionString = "Server=MAX\\SQLEXPRESS;Database=AdminExIdentityTestDb;" +
           "Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

        private Exception? _exception;

        public ExceptionMiddlewareFixture() : base("", _identityConnectionString, useDatabase: false) { }        

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

            services.RemoveAll(typeof(IPostService));
            services.AddScoped<IPostService>(_ => new PostServiceMock(_exception));

            services.RemoveAll(typeof(ICommentService));
            services.AddScoped<ICommentService>(_ => new CommentServiceMock(_exception));

            services.RemoveAll(typeof(IAuthService));
            services.AddScoped<IAuthService>(_ => new AuthServiceMock(_exception));

            services.RemoveAll(typeof(ITokenService));
            services.AddScoped<ITokenService>(_ => new TokenServiceMock(_exception));
        }

        public void SetCurrentUser(ClaimsPrincipal user)
        {
            DynamicAuthHandler.CurrentPrincipal = user;
        }

        public void SetException(Exception exception)
        {
            _exception = exception;
        }

        public override async Task DisposeAsync()
        {
            using var scope = Services.CreateScope();
            var identityContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

            await identityContext.Database.EnsureDeletedAsync();

            await base.DisposeAsync();
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
