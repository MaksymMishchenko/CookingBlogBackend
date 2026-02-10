using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Security.Claims;
using System.Threading.RateLimiting;
public static class ServiceCollectionExtensions
{
    public static void AddTestDatabase(this IServiceCollection services, string connectionString)
    {
        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
    }

    public static void AddTestAuth(this IServiceCollection services)
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

    public static IServiceCollection AddExceptionMiddlewareMocks(this IServiceCollection services)
    {
        services.RemoveAll(typeof(IAuthService));
        var authServiceMock = Substitute.For<IAuthService>();

        var dto = AuthTestData.CreateLoggedInUserDto();

        authServiceMock.AuthenticateAsync(Arg.Any<LoginUserDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<LoggedInUserDto>.Success(dto,
                string.Format(Auth.LoginM.Success.LoginSuccess, dto.UserName))));

        authServiceMock.RegisterUserAsync(Arg.Any<RegisterUserDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<RegisteredUserDto>.Created(
                new RegisteredUserDto("test-id", "test-name", "test-email"),
            Auth.Registration.Success.RegisterOk)));

        services.AddScoped(_ => authServiceMock);

        services.RemoveAll(typeof(ITokenService));
        var tokenServiceMock = Substitute.For<ITokenService>();

        tokenServiceMock.GenerateTokenString(Arg.Any<IEnumerable<Claim>>())
            .Returns("mock-token");

        services.AddScoped(_ => tokenServiceMock);

        return services;
    }

    public static IServiceCollection DisableRateLimiting(this IServiceCollection services)
    {        
        services.RemoveAll(typeof(IConfigureOptions<RateLimiterOptions>));

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(RateLimitOptions.PolicyName, context =>
                RateLimitPartition.GetNoLimiter("BypassAll"));
        });

        return services;
    }

    public static void ClearAllMocks(this IServiceProvider services)
    {
        services.GetRequiredService<IAuthService>().ClearReceivedCalls();
        services.GetRequiredService<ITokenService>().ClearReceivedCalls();
    }
}