using Microsoft.Extensions.DependencyInjection;
using PostApiService.Helper;
using PostApiService.Models.TypeSafe;
using System.Security.Claims;

namespace PostApiService.Tests.Helper
{
    public static class DataSeedingExtensions
    {
        public static async Task SeedBlogDataAsync(
            this IServiceProvider services,
            IEnumerable<Post> posts,
            IEnumerable<Category> categories)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (categories.Any()) await dbContext.Categories.AddRangeAsync(categories);
            if (posts.Any()) await dbContext.Posts.AddRangeAsync(posts);

            await dbContext.SaveChangesAsync();
        }

        public static async Task SeedCategoriesAsync(
            this IServiceProvider services,
            IEnumerable<Category> categories)
        {
            using (var scope = services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Categories.AddRangeAsync(categories);

                await dbContext.SaveChangesAsync();
            }
        }

        private static Claim GetAdminClaims(string controllerName)
        {
            return new Claim(controllerName, ClaimHelper.SerializePermissions(
                TS.Permissions.Write,
                TS.Permissions.Update,
                TS.Permissions.Delete
            ));
        }

        public static async Task SeedAdminAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(TS.Roles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(TS.Roles.Admin));
            }

            var admin = await userManager.FindByIdAsync(TestUserData.AdminId);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    Id = TestUserData.AdminId,
                    UserName = TestUserData.AdminUserName,
                    Email = "admin@test.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, TestUserData.AdminPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create test admin user: {errors}");
                }

                await userManager.AddToRoleAsync(admin, TS.Roles.Admin);

                await userManager.AddClaimsAsync(admin, new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, admin.Id),
                    new Claim(ClaimTypes.Name, admin.UserName!),
                    GetAdminClaims(TS.Controller.Post),
                    GetAdminClaims(TS.Controller.Category),
                    GetAdminClaims(TS.Controller.Comment)
                });
            }
        }
    }
}
