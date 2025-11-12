using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PostApiService.Contexts;
using PostApiService.Interfaces;
using PostApiService.Models;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PostApiService.Tests.Fixtures
{
    public class ExceptionMiddlewareFixture : BaseTestFixture
    {
        private const string _identityConnectionString = "Server=MAX\\SQLEXPRESS;Database=AdminExIdentityTestDb;" +
           "Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

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

            var postServiceMock = Substitute.For<IPostService>();
            var mockPosts = new List<Post> {
                new Post { Title = "Mocked Post" }
            };

            const int mockTotalCount = 100;

            postServiceMock.GetPostsWithTotalAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((
                    Posts: mockPosts,         
                    TotalCount: mockTotalCount
                )));            

            postServiceMock.GetPostByIdAsync(
                Arg.Any<int>(),
                Arg.Any<bool>())
                .Returns(Task.FromResult(new Post { Title = "Mocked Post" }));

            postServiceMock.AddPostAsync(
                Arg.Any<Post>())
                .Returns(Task.FromResult(new Post { Title = "Mocked Post" }));

            postServiceMock.UpdatePostAsync(
                Arg.Any<int>(),
                Arg.Any<Post>())
                .Returns(Task.CompletedTask);

            postServiceMock.DeletePostAsync(
                Arg.Any<int>())
                .Returns(Task.CompletedTask);

            services.AddScoped(_ => postServiceMock);

            services.RemoveAll(typeof(ICommentService));

            var commentServiceMock = Substitute.For<ICommentService>();
            commentServiceMock.AddCommentAsync(
                Arg.Any<int>(),
                Arg.Any<Comment>())
                .Returns(Task.CompletedTask);

            commentServiceMock.UpdateCommentAsync(
                Arg.Any<int>(),
                Arg.Any<EditCommentModel>())
                .Returns(Task.CompletedTask);

            commentServiceMock.DeleteCommentAsync(
                Arg.Any<int>())
                .Returns(Task.CompletedTask);

            services.AddScoped(_ => commentServiceMock);

            services.RemoveAll(typeof(IAuthService));
            var authServiceMock = Substitute.For<IAuthService>();

            authServiceMock.GenerateTokenString(
                Arg.Any<IdentityUser>())
                .Returns(Task.FromResult("mocked_token"));

            authServiceMock.GetCurrentUserAsync()
                .Returns(Task.FromResult(new IdentityUser { UserName = "testUser" }));

            authServiceMock.LoginAsync(Arg.Any<LoginUser>())
                .Returns(Task.FromResult(new IdentityUser { UserName = "testUser" }));

            authServiceMock.RegisterUserAsync(Arg.Any<RegisterUser>())
                .Returns(Task.CompletedTask);

            services.AddScoped(_ => authServiceMock);

            services.RemoveAll(typeof(ITokenService));
            var tokenServiceMock = Substitute.For<ITokenService>();

            tokenServiceMock.GenerateTokenString(Arg.Any<IEnumerable<Claim>>())
                .Returns("");

            services.AddScoped(_ => tokenServiceMock);
        }

        public void SetCurrentUser(ClaimsPrincipal user)
        {
            DynamicAuthHandler.CurrentPrincipal = user;
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