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
                Description = "Description for first post",
                Content = "This is the content of the first post.",
                Author = "Peter Jack",
                CreateAt = DateTime.Now,
                ImageUrl = "/images/placeholder.jpg",
                MetaTitle = "Meta title info",
                MetaDescription = "This is meta description",
                Slug = "http://localhost:4200/first-post"
            },
            new Post
            {
                PostId = 2,
                Title = "Second Post",
                Description = "Description for second post",
                Content = "This is the content of the second post.",
                Author = "Jay Way",
                CreateAt = DateTime.Now,
                ImageUrl = "/images/placeholder.jpg",
                MetaTitle = "Meta title info 2",
                MetaDescription = "This is meta description 2",
                Slug = "http://localhost:4200/second-post"
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
