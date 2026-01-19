using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Security.Claims;
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
        services.RemoveAll(typeof(IPostService));

        var postServiceMock = Substitute.For<IPostService>();
        postServiceMock.AddPostAsync(
            Arg.Any<PostCreateDto>())
            .Returns(Task.FromResult(Result<PostAdminDetailsDto>.Success(new PostAdminDetailsDto(
                1, "Mocked Post", "Mock Desc", "Content", "Author", "ImageUrl", "Slug", "Meta Title",
                "Meta Desc", 1, DateTime.UtcNow, null))));

        services.AddScoped(_ => postServiceMock);

        services.RemoveAll(typeof(ICommentService));
        var commentServiceMock = Substitute.For<ICommentService>();

        var expectedDto = new CommentCreatedDto(
            1,
            "Author",
            "Content",
            DateTime.UtcNow,
            "testUserId"
            );

        commentServiceMock.AddCommentAsync(Arg.Any<int>(), Arg.Any<string>())
            .Returns(Task.FromResult(Result<CommentCreatedDto>.Success(expectedDto,
                CommentM.Success.CommentAddedSuccessfully)));

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

        return services;
    }

    public static void ClearAllMocks(this IServiceProvider services)
    {
        services.GetRequiredService<IAuthService>().ClearReceivedCalls();
        services.GetRequiredService<IPostService>().ClearReceivedCalls();
        services.GetRequiredService<ICommentService>().ClearReceivedCalls();
        services.GetRequiredService<ITokenService>().ClearReceivedCalls();
    }
}