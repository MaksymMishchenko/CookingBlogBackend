using Microsoft.EntityFrameworkCore;
using PostApiService.Dto;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;

        public PostService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Asynchronously adds a new post to the database and sets its creation timestamp.
        /// </summary>
        /// <param name="post">The post object to be added, which must include the required fields.</param>
        /// <remarks>
        /// This method sets the <see cref="Post.CreateAt"/> property to the current date and time 
        /// before adding the post to the database context and saving the changes.
        /// </remarks>
        public async Task AddPostAsync(Post post)
        {
            post.CreateAt = DateTime.Now;
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePostAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);

            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task EditPostAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PostDto>> GetAllPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.Comments)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Description = p.Description,
                    Content = p.Content,
                    Author = p.Author,
                    CreateAt = p.CreateAt,
                    ImageUrl = p.ImageUrl,
                    MetaTitle = p.Title,
                    MetaDescription = p.MetaDescription,
                    Slug = p.Slug,
                    Comments = p.Comments.Select(c => new CommentDto
                    {
                        CommentId = c.CommentId,
                        Author = c.Author,
                        Content = c.Content,
                        CreateAt = c.CreatedAt
                    }).ToList()

                }).ToListAsync();
        }

        public async Task<PostDto> GetPostByIdAsync(int postId)
        {
            return await _context.Posts
            .Where(p => p.PostId == postId)
            .Include(p => p.Comments)
            .Select(p => new PostDto
            {
                PostId = p.PostId,
                Title = p.Title,
                Content = p.Content,
                CreateAt = p.CreateAt,
                Comments = p.Comments.Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    Author = c.Author,
                    Content = c.Content,
                    CreateAt = c.CreatedAt
                }).ToList()

            }).FirstOrDefaultAsync();
        }
    }
}
