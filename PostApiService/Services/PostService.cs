using Microsoft.EntityFrameworkCore;
using PostApiService.Dto;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostService> _logger;

        public PostService(ApplicationDbContext context, ILogger<PostService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adds a new post to the database asynchronously and returns the result status.
        /// </summary>
        /// <param name="post">The <see cref="Post"/> object to be added.</param>
        /// <returns>
        /// A tuple indicating the success of the operation and the ID of the added post:
        /// - Success: A boolean indicating whether the operation was successful.
        /// - PostId: The ID of the newly added post if successful, or 0 if the operation failed.
        /// </returns>
        /// <exception cref="DbUpdateException">
        /// Thrown when a database-related error occurs during the save operation.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when an unexpected error occurs.
        /// </exception>
        /// <remarks>
        /// This method validates the provided <see cref="Post"/> object before attempting to add it to the database.
        /// It logs the outcome of the operation, including success, failure, or any errors encountered.
        /// </remarks>
        public async Task<(bool Success, int PostId)> AddPostAsync(Post post)
        {
            ValidatePost(post);

            try
            {
                await _context.Posts.AddAsync(post);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Post was added successfuly");
                    return (true, post.PostId);
                }
                _logger.LogWarning($"Failed to add post with title: {post.Title}");
                return (false, 0);
            }

            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while adding post: {Post}.", post);
                throw; 
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while adding a post: {post}");
                throw;
            }
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

        public async Task<Post> GetPostByIdAsync(int postId)
        {
            return await _context.Posts
            .Where(p => p.PostId == postId)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync();
        }

        private void ValidatePost(Post post)
        {
            if (post == null)
            {
                _logger.LogError($"Attempted to add a null post: {post}");
                throw new ArgumentNullException(nameof(post), "Post cannot be null.");
            }            
        }
    }
}
