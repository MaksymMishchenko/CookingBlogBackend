using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

public class DynamicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DynamicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue(TestUserData.TestUserHeader, out var userType))
        {
            return Task.FromResult(AuthenticateResult.Fail("No test user header found."));
        }

        ClaimsPrincipal? principal = userType.ToString() switch
        {
            TestUserData.AdminKey => TestUserBuilder.CreateAdmin(),
            TestUserData.ContributorKey => TestUserBuilder.CreateContributor(),
            TestUserData.Contributor2Key => TestUserBuilder.CreateContributor2(),
            _ => null
        };

        if (principal == null) return Task.FromResult(AuthenticateResult.Fail("Unknown test user type."));

        var ticket = new AuthenticationTicket(principal, "DynamicScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}