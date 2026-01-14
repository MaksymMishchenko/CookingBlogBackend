namespace PostApiService.Repositories
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task<Comment?> GetWithUserAsync(int id, CancellationToken ct = default);
    }
}
