using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IUserService
    {
        Task<Result<List<AuthorsDto>>> GetAdminAndContributorUsersAsync(CancellationToken ct = default);
    }
}
