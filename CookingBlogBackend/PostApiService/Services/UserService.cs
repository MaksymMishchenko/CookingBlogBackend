using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class UserService: BaseResultService, IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Retrieves all Admin and Contributor role users and projects them into lightweight DTOs.
        /// </summary>
        public async Task<Result<List<AuthorsDto>>> GetAdminAndContributorUsersAsync(CancellationToken ct = default)
        {
            var adminUsers = await _userRepository.GetAdminAndContributorUsersAsync(ct);

            var adminDtos = adminUsers
                .Select(u => new AuthorsDto(u.Id, u.UserName!))
                .ToList();

            return Success(adminDtos, Auth.AdminM.Success.ContributorsRetrievedSuccessfully);
        }
    }
}
