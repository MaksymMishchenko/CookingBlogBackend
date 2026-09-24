namespace PostApiService.Repositories
{
    public interface IUserRepository
    {
        Task<List<IdentityUser>> GetAdminAndContributorUsersAsync(CancellationToken ct = default);
    }
}
