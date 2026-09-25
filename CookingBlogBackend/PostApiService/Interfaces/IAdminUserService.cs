using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IAdminUserService
    {
        Task<Result<List<AuthorsDto>>> GetAdminAndContributorUsersAsync(CancellationToken ct = default);
    }
}
