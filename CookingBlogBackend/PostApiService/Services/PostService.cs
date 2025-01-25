using Microsoft.EntityFrameworkCore;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Services
{
    public class PostService : IPostService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<PostService> _logger;

        public PostService(IApplicationDbContext context, ILogger<PostService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paginated list of posts from the database, with optional inclusion of comments.
        /// </summary>
        /// <param name="pageNumber">The number of the page to retrieve, starting from 1.</param>
        /// <param name="pageSize">The number of posts per page.</param>
        /// <param name="commentPageNumber">The page number for comments pagination (default is 1).</param>
        /// <param name="commentsPerPage">The number of comments to retrieve per post (default is 10).</param>
        /// <param name="includeComments">Indicates whether to include comments for each post (default is true).</param>
        /// <returns>A list of posts, optionally with paginated comments.</returns>
        /// <exception cref="Exception">
        /// Thrown when an unexpected error occurs while fetching posts from the database.
        /// The exception message contains details about the request parameters for debugging.
        /// </exception>
        /// <remarks>
        /// If comments are included, they are paginated for each post according to the provided
        /// <paramref name="commentPageNumber"/> and <paramref name="commentsPerPage"/> parameters.
        /// </remarks>
        public async Task<List<Post>> GetAllPostsAsync(int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true)
        {
            var query = _context.Posts.AsQueryable();

            if (includeComments)
            {
                query = query.Include(p => p.Comments);
            }

            try
            {
                var posts = await query
                    .OrderBy(p => p.PostId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} posts. Total posts", posts.Count);

                ProcessComments(posts, includeComments, commentPageNumber, commentsPerPage);

                return posts;
            }
            catch (Exception ex)
            {
                var detailedMessage = $"An unexpected error occurred while fetching posts from the database. PageNumber: {pageNumber}, PageSize: {pageSize}, IncludeComments: {includeComments}.";
                _logger.LogError(ex, detailedMessage);
                throw new Exception(detailedMessage, ex);
            }

            void ProcessComments(List<Post> posts, bool includeComments, int commentPageNumber, int commentsPerPage)
            {
                if (includeComments)
                {
                    _logger.LogInformation("Fetching comments from the database. Page: {Page}, Size: {Size}.", commentPageNumber, commentsPerPage);

                    foreach (var post in posts)
                    {
                        post.Comments = post.Comments
                            .OrderBy(c => c.CreatedAt)
                            .Skip((commentPageNumber - 1) * commentsPerPage)
                            .Take(commentsPerPage)
                            .ToList();
                    }
                }
                else
                {
                    foreach (var post in posts)
                    {
                        post.Comments = new List<Comment>();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a post by its ID from the database. Optionally includes comments associated with the post.
        /// </summary>
        /// <param name="postId">The ID of the post to retrieve.</param>
        /// <param name="includeComments">Indicates whether to include the comments related to the post. Default is true.</param>
        /// <returns>
        /// The post with the specified ID, including its comments if <paramref name="includeComments"/> is true.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when a post with the specified ID does not exist in the database.
        /// </exception>
        /// <remarks>
        /// Logs a warning if the post is not found and an informational message if the post is successfully retrieved.
        /// </remarks>
        public async Task<Post> GetPostByIdAsync(int postId, bool includeComments = true)
        {
            var query = _context.Posts.AsQueryable();

            if (includeComments)
            {
                query = query.Include(p => p.Comments);
            }

            var post = await query.FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null)
            {
                _logger.LogWarning("Post with ID {postId} not found.", postId);
                throw new KeyNotFoundException($"Post with ID {postId} was not found.");
            }

            _logger.LogInformation("Successfully fetched post with ID {postId}.", postId);

            return post;
        }

        /// <summary>
        /// Asynchronously adds a new post to the database if the post title does not already exist.
        /// If a post with the same title exists, a <see cref="DbUpdateException"/> is thrown.
        /// Logs success or failure depending on whether the post was added successfully.
        /// </summary>
        /// <param name="post">The post object to be added to the database.</param>
        /// <returns>
        /// Returns <c>true</c> if the post was added successfully, otherwise <c>false</c> if the post was not added.
        /// Throws a <see cref="DbUpdateException"/> if the post title already exists in the database.
        /// </returns>
        /// <exception cref="DbUpdateException">Thrown when a post with the same title already exists.</exception>
        /// <exception cref="Exception">Thrown for any unexpected errors during the database operation.</exception>
        public async Task<bool> AddPostAsync(Post post)
        {
            var existingPost = await _context.Posts
            .AnyAsync(p => p.Title == post.Title);

            if (existingPost)
            {
                _logger.LogWarning("A post with the title '{Title}' already exists.", post.Title);

                throw new DbUpdateException("A post with this title already exists.");
            }

            await _context.Posts.AddAsync(post);

            try
            {
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Post was added successfully.");
                    return true;
                }
                _logger.LogWarning($"Failed to add post with title: {post.Title}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "An unexpected error occurred while adding post to database");

                throw new Exception("An unexpected error occurred while adding post to database.");
            }
        }

        /// <summary>
        /// Deletes a post from the database based on the specified post ID.
        /// </summary>
        /// <param name="postId">The unique identifier of the post to be deleted.</param>
        /// <returns>
        /// A boolean value indicating the success of the operation. 
        /// Returns <c>true</c> if the post was successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method performs the following steps:
        /// 1. Validates the input post ID.
        /// 2. Checks if the post exists in the database using <see cref="FindAsync"/>.
        /// 3. If the post exists, it is removed from the context and changes are saved to the database.
        /// 4. Logs appropriate messages based on the outcome of the operation.
        /// 5. Handles database-specific errors using <see cref="DbUpdateException"/> and logs unexpected errors.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the provided post ID is invalid.</exception>
        /// <exception cref="Exception">Rethrows unexpected exceptions after logging.</exception>
        public async Task<bool> DeletePostAsync(int postId)
        {
            ValidatePost(postId);

            try
            {
                var postExist = await _context.Posts.FindAsync(postId);

                if (postExist == null)
                {
                    _logger.LogWarning("Post with ID {PostId} does not exist. Deletion aborted.", postId);
                    return false;
                }

                _context.Posts.Remove(postExist);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Post with ID {PostId} was successfully deleted.", postId);
                    return true;
                }

                _logger.LogWarning("No rows were affected when attempting to delete post with ID {PostId}.", postId);
                return false;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while deleting post with ID {PostId}.", postId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting post with ID {PostId}.", postId);
                throw;
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
                    //_context.Entry(post).Property(propertyName).IsModified = true;
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

        /// <summary>
        /// Validates the specified post object to ensure it is not null.
        /// Logs an error and throws an <see cref="ArgumentNullException"/> if the post is null.
        /// </summary>
        /// <param name="post">The post object to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="post"/> is null.</exception>
        private void ValidatePost(Post post)
        {
            if (post == null)
            {
                _logger.LogError($"Attempted to add a null post: {post}");
                throw new ArgumentNullException(nameof(post), "Post cannot be null.");
            }
        }

        /// <summary>
        /// Validates the specified post ID to ensure it is greater than zero.
        /// Logs an error and throws an <see cref="ArgumentException"/> if the post ID is invalid.
        /// </summary>
        /// <param name="postId">The ID of the post to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="postId"/> is less than or equal to zero.</exception>
        private void ValidatePost(int postId)
        {
            if (postId <= 0)
            {
                _logger.LogError("Invalid post ID: {PostId}", postId);
                throw new ArgumentException("Invalid post ID.", nameof(postId));
            }
        }
    }
}
