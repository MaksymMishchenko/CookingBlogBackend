using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class AdminUserService : AdminBaseService, IAdminUserService
    {
        private readonly IUserRepository _userRepository;

        public AdminUserService(IUserRepository userRepository, IWebContext webContext) : base(webContext)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Retrieves all Admin and Contributor role users and projects them into lightweight DTOs.
        /// </summary>
        public async Task<Result<List<AuthorsDto>>> GetAdminAndContributorUsersAsync(CancellationToken ct = default)
        {
            var accessError = ValidateAdminOnly<List<AuthorsDto>>();
            if (accessError != null) return accessError;

            var adminUsers = await _userRepository.GetAdminAndContributorUsersAsync(ct);

            var adminDtos = adminUsers
                .Select(u => new AuthorsDto(u.Id, u.UserName!))
                .ToList();

            return Success(adminDtos, UserM.Success.AdminAndContributorUsersRetrievedSuccessfully);
        }
    }
}
