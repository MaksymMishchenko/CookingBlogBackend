using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IAuthService
    {
        Task<Result<RegisteredUserDto>> RegisterUserAsync(
            RegisterUserDto userDto, CancellationToken ct = default);
        Task<Result<LoggedInUserDto>> AuthenticateAsync(LoginUserDto credentials,
            CancellationToken ct = default);
    }
}
