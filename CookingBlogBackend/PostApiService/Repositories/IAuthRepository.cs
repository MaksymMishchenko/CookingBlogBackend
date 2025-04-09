using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace PostApiService.Repositories
{
    public interface IAuthRepository
    {
        Task<IdentityUser> FindByNameAsync(string userName);
        Task<IdentityUser> FindByEmailAsync(string email);
        Task<IdentityResult> CreateAsync(IdentityUser user, string password);
        Task<IdentityResult> AddClaimAsync(IdentityUser user, Claim claim);
        Task<IList<Claim>> GetClaimsAsync(IdentityUser user);
        Task<IList<string>> GetRolesAsync(IdentityUser user);
        Task<bool> CheckPasswordAsync(IdentityUser user, string password);
        Task<IdentityUser> GetUserAsync();
    }
}
