using Microsoft.Extensions.DependencyInjection;
using PostApiService.Helper;
using PostApiService.Models.TypeSafe;
using System.Security.Claims;

public static class TestUserBuilder
{
    private static IEnumerable<Claim> ExpandPermissions(string type, Claim serializedClaim)
    {
        var permissions = serializedClaim.DeserializePermissions();
        return permissions.Select(p => new Claim(type, p.ToString()));
    }

    public static ClaimsPrincipal CreateAdmin(string controller = TS.Controller.Post)
    {
        var serializedClaim = GetAdminPermissionsClaim(controller);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserData.AdminId),
            new Claim(ClaimTypes.Name, TestUserData.AdminUserName),
            new Claim(ClaimTypes.Role, TS.Roles.Admin)
        };

        claims.AddRange(ExpandPermissions(controller, serializedClaim));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "DynamicScheme", ClaimTypes.Name, ClaimTypes.Role));
    }
    // ClaimTypes.Name, ClaimTypes.Role
    public static ClaimsPrincipal CreateContributor(string controller = TS.Controller.Comment)
    {
        var serializedClaim = GetContributorPermissionsClaim(controller);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserData.ContributorId),
            new Claim(ClaimTypes.Name, TestUserData.ContributorUserName),
            new Claim(ClaimTypes.Role, TS.Roles.Contributor)
        };

        claims.AddRange(ExpandPermissions(controller, serializedClaim));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "DynamicScheme", ClaimTypes.Name, ClaimTypes.Role));
    }

    public static ClaimsPrincipal CreateContributor2(string controller = TS.Controller.Comment)
    {
        var serializedClaim = GetContributorPermissionsClaim(controller);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserData.Contributor2Id),
            new Claim(ClaimTypes.Name, TestUserData.Contributor2UserName),
            new Claim(ClaimTypes.Role, TS.Roles.Contributor)
        };

        claims.AddRange(ExpandPermissions(controller, serializedClaim));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "DynamicScheme", ClaimTypes.Name, ClaimTypes.Role));
    }


    public static Claim GetAdminPermissionsClaim(string controllerName)
    {
        return new Claim(controllerName, ClaimHelper.SerializePermissions(
            TS.Permissions.Write, TS.Permissions.Update, TS.Permissions.Delete));
    }

    public static Claim GetContributorPermissionsClaim(string controllerName)
    {
        return new Claim(controllerName, ClaimHelper.SerializePermissions(
            TS.Permissions.Write, TS.Permissions.Update, TS.Permissions.Delete));
    }

    public static async Task SeedDefaultUsersAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(TS.Roles.Admin))
            await roleManager.CreateAsync(new IdentityRole(TS.Roles.Admin));

        if (!await roleManager.RoleExistsAsync(TS.Roles.Contributor))
            await roleManager.CreateAsync(new IdentityRole(TS.Roles.Contributor));        

        var adminUser = new IdentityUser
        {
            Id = TestUserData.AdminId,
            UserName = TestUserData.AdminUserName,
            Email = "admin@test.com"
        };
        await EnsureUserCreatedAsync(userManager, adminUser, TestUserData.AdminPassword, TS.Roles.Admin,
            GetAdminPermissionsClaim(TS.Controller.Post), GetAdminPermissionsClaim(TS.Controller.Comment));

        var contributorUser = new IdentityUser
        {
            Id = TestUserData.ContributorId,
            UserName = TestUserData.ContributorUserName,
            Email = "c@test.com"
        };
        await EnsureUserCreatedAsync(userManager, contributorUser, TestUserData.ContributorPassword, TS.Roles.Contributor,
            GetContributorPermissionsClaim(TS.Controller.Comment));

        var contributorUser2 = new IdentityUser
        {
            Id = TestUserData.Contributor2Id,
            UserName = TestUserData.Contributor2UserName,
            Email = "c2@test.com"
        };
        await EnsureUserCreatedAsync(userManager, contributorUser2, TestUserData.Contributor2Password, TS.Roles.Contributor,
            GetContributorPermissionsClaim(TS.Controller.Comment));
    }

    private static async Task EnsureUserCreatedAsync(
        UserManager<IdentityUser> userManager,
        IdentityUser user,
        string password,
        string role,
        params Claim[] permissionClaims)
    {
        var existingUser = await userManager.FindByIdAsync(user.Id);
        if (existingUser == null)
        {
            await userManager.CreateAsync(user, password);
            await userManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id));

            foreach (var claim in permissionClaims)
            {
                await userManager.AddClaimAsync(user, claim);
            }

            await userManager.AddToRoleAsync(user, role);
        }
    }
}