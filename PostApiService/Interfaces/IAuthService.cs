using Microsoft.AspNetCore.Identity;
using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Token, DateTime Expiration)> LoginAsync(LoginModel model);
    }
}
