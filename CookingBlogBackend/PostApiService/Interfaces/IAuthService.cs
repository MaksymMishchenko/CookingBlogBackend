using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IAuthService
    {
        Task<string> GenerateTokenString(string username, JwtConfiguration config);
        Task LoginAsync(LoginUser model);
    }
}
