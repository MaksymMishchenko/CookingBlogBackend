using System.Security.Claims;

namespace PostApiService.Repositories
{
    public interface IAuthRepository
    {
        Task<IdentityUser?> FindByNameAsync(string userName, CancellationToken ct = default);
        Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken ct = default);
        Task<IdentityResult> CreateAsync(IdentityUser user, string password, CancellationToken ct = default);
        Task<IdentityResult> AddClaimAsync(IdentityUser user, Claim claim, CancellationToken ct = default);
        Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken ct = default);
        Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken ct = default);
        Task<bool> CheckPasswordAsync(IdentityUser user, string password, CancellationToken ct = default);        
    }
}
