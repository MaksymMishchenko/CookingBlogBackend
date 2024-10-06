using Microsoft.EntityFrameworkCore;
using PostApiService.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed posts
        modelBuilder.Entity<Post>().HasData(
            new Post
            {
                PostId = 1,
                Title = "First Post",
                Content = "This is the content of the first post.",
                CreateAt = DateTime.Now
            },
            new Post
            {
                PostId = 2,
                Title = "Second Post",
                Content = "This is the content of the second post.",
                CreateAt = DateTime.Now
            }
        );

        // Seed comments for the posts
        modelBuilder.Entity<Comment>().HasData(
            new Comment
            {
                CommentId = 1,
                Author = "John Doe",
                Content = "Great post!",
                CreatedAt = DateTime.Now,
                PostId = 1 // Link this comment to PostId 1
            },
            new Comment
            {
                CommentId = 2,
                Author = "Jane Doe",
                Content = "I totally agree with this!",
                CreatedAt = DateTime.Now,
                PostId = 1 // Link this comment to PostId 1
            },
            new Comment
            {
                CommentId = 3,
                Author = "Alice",
                Content = "This is a comment on the second post.",
                CreatedAt = DateTime.Now,
                PostId = 2 // Link this comment to PostId 2
            }
        );

        base.OnModelCreating(modelBuilder);
    }
}
