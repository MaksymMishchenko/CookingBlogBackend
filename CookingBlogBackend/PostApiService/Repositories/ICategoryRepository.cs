namespace PostApiService.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<string?> GetNameBySlugAsync(string? slug, CancellationToken ct);
    }
}
