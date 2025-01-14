using Microsoft.EntityFrameworkCore;
using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Post> Posts { get; }
        DbSet<Comment> Comments { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
