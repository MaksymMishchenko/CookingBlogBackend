using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace PostApiService.Repositories
{
    public interface IAuthRepository
    {
        Task<IdentityUser> FindByNameAsync(string userName);
        Task<IdentityUser> FindByEmailAsync(string email);
        Task<IdentityUser> CreateAsync(IdentityUser user, string password);
        Task<IdentityResult> AddClaimsAsync(IdentityUser user, Claim claim);
        Task<IList<Claim>> GetClaimsAsync(IdentityUser user);
        Task<IList<string>> GetRolesAsync(IdentityUser user);
        Task<bool> CheckPasswordAsync(IdentityUser user, string password);
        Task<IdentityUser> GetUserAsync(ClaimsPrincipal principal);
    }
}
