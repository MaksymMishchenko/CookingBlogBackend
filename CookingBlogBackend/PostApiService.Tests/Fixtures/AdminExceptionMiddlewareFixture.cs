﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostApiService.Interfaces;
using PostApiService.Tests.Mocks;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PostApiService.Tests.Fixtures
{
    public class AdminExceptionMiddlewareFixture : BaseTestFixture
    {
        private Exception? _exception;

        public AdminExceptionMiddlewareFixture() : base("", useDatabase: false) { }

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
