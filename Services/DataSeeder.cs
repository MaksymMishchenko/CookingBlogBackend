using PostApiService.Models;

namespace PostApiService.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;

        public DataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedDataAsync()
        {
            if (!_context.Posts.Any())
            {
                var posts = new List<Post>
            {
                new Post
                {
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
            };
                await _context.Posts.AddRangeAsync(posts);
                await _context.SaveChangesAsync();

                var comments = new List<Comment>
            {
                new Comment
                {
                    Author = "John Doe",
                    Content = "Great post!",
                    CreatedAt = DateTime.Now,
                    PostId = posts[0].PostId
                },
                new Comment
                {
                    Author = "Jane Doe",
                    Content = "I totally agree with this!",
                    CreatedAt = DateTime.Now,
                    PostId = posts[0].PostId
                },
                new Comment
                {
                    Author = "Alice",
                    Content = "This is a comment on the second post.",
                    CreatedAt = DateTime.Now,
                    PostId = posts[1].PostId
                }
            };
                await _context.Comments.AddRangeAsync(comments);
                await _context.SaveChangesAsync();
            }
        }
    }
}