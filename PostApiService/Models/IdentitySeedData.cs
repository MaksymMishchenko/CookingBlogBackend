using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace PostApiService.Models
{
    public static class IdentitySeedData
    {
        private const string _adminUser = "Admin";
        private const string _adminPassword = "~Rtyuehe8";

        public static async Task EnsurePopulatedAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                var user = await userManager.FindByNameAsync(_adminUser);

                if (user == null)
                {
                    user = new IdentityUser(_adminUser);
                    var result = await userManager.CreateAsync(user, _adminPassword);

                    if (result.Succeeded)
                    {
                        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, user.UserName));
                        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Admin"));
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error: {error.Description}");
                        }
                    }
                }
                else
                {
                    // Check if the user already has these claims and add if they are missing
                    var claims = await userManager.GetClaimsAsync(user);
                    if (!claims.Any(c => c.Type == ClaimTypes.Name))
                    {
                        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, user.UserName));
                    }
                    if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
                    {
                        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Admin"));
                    }
                }
            }
        }
    }
}
