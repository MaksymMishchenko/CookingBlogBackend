namespace PostApiService.Repositories
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        private readonly ApplicationDbContext _context;

        public CommentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<Comment?> GetWithUserAsync(int id, CancellationToken ct = default)
        {
            return await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }
    }
}
