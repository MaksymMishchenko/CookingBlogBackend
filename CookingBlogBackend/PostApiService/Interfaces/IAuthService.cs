using Microsoft.AspNetCore.Identity;
using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IAuthService
    {
        Task RegisterUserAsync(RegisterUser user);
        Task<IdentityUser> LoginAsync(LoginUser credentials);

        Task<IdentityUser> GetCurrentUserAsync();
        Task<string> GenerateTokenString(IdentityUser user);
    }
}
