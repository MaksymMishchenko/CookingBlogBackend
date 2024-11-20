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
                    _logger.LogInformation("Post was added successfully");
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

        /// <summary>
        /// Updates an existing post in the database with the provided data.
        /// Only specified properties of the post will be updated.
        /// </summary>
        /// <param name="post">The post object containing the updated data. Must include a valid PostId.</param>
        /// <returns>
        /// A tuple containing:
        /// - <c>Success</c>: <c>true</c> if the update was successful; <c>false</c> otherwise.
        /// - <c>PostId</c>: The ID of the updated post if successful; <c>0</c> if not.
        /// </returns>
        /// <remarks>
        /// The method performs the following steps:
        /// 1. Validates the input post object.
        /// 2. Checks if a post with the given ID exists in the database.
        /// 3. If the post exists, updates only the specified properties:
        ///    - Title
        ///    - Content
        ///    - Author
        ///    - Description
        ///    - MetaTitle
        ///    - MetaDescription
        ///    - ImageUrl
        /// 4. Saves the changes to the database.
        /// 5. Logs the outcome, including errors or warnings in case of failure.
        /// 
        /// Exceptions:
        /// - Throws <see cref="DbUpdateException"/> if there is a failure while saving to the database.
        /// - Throws other exceptions if unexpected errors occur.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the post object is invalid.</exception>
        /// <exception cref="DbUpdateException">Thrown if database update fails.</exception>
        /// <exception cref="Exception">Thrown for other unexpected errors.</exception>
        public async Task<(bool Success, int PostId)> EditPostAsync(Post post)
        {
            ValidatePost(post);

            try
            {               
                var postExists = await _context.Posts.AsNoTracking()
                    .AnyAsync(p => p.PostId == post.PostId);
                if (!postExists)
                {
                    _logger.LogWarning("Post with ID {PostId} does not exist. Cannot edit.", post.PostId);
                    return (false, 0);
                }
                
                _context.Posts.Attach(post);
                var propertiesToUpdate = new[] { "Title", "Content", "Author", "Description", "MetaTitle", "MetaDescription", "ImageUrl" };
                
                foreach (var propertyName in propertiesToUpdate)
                {
                    _context.Entry(post).Property(propertyName).IsModified = true;
                }
                
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Successfully updated post with ID {PostId}", post.PostId);
                    return (true, post.PostId);
                }

                _logger.LogWarning("Failed to update post with ID {PostId}", post.PostId);
                return (false, 0);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed for post with ID {PostId}", post.PostId);
                return (false, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while editing post with ID {PostId}", post.PostId);
                throw;
            }
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
