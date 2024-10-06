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

        public async Task<List<PostDto>> GetAllPosts()
        {
            return await _context.Posts
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

                }).ToListAsync();
        }
    }
}
