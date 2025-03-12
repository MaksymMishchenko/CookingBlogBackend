using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IAuthService
    {
        Task RegisterUserAsync(RegisterUser user);
    }
}
