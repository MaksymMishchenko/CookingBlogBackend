namespace PostApiService.Repositories
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(ApplicationDbContext context) : base(context) { }

        public async Task<bool> IsAvailableForCommentingAsync(int postId, CancellationToken ct)
        {
            return await _dbSet.AnyAsync(p => p.Id == postId && p.IsActive, ct);
        }
    }
}
