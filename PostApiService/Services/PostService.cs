using Microsoft.EntityFrameworkCore;
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
                // Логування помилок, пов’язаних із базою даних
                _logger.LogError(dbEx, "Database error occurred while deleting post with ID {PostId}.", postId);
                return false;
            }
            catch (Exception ex)
            {
                // Логування будь-яких інших помилок
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

        /// <summary>
        /// Retrieves a paginated list of posts, with optional pagination for comments.
        /// Logs warnings for invalid parameters and includes comments if specified.
        /// Defaults to empty lists for comments when not included or when an error occurs.
        /// </summary>
        /// <param name="pageNumber">The page number for the posts. Defaults to 1 if less than 1.</param>
        /// <param name="pageSize">The number of posts per page. Defaults to 10 if less than 1.</param>
        /// <param name="commentPageNumber">The page number for comments within each post. Defaults to 1 if less than 1.</param>
        /// <param name="commentsPerPage">The number of comments per page for each post. Defaults to 10 if less than 1.</param>
        /// <param name="includeComments">Determines whether to include comments in the result. Defaults to true.</param>
        /// <returns>
        /// A list of posts, each optionally including a paginated subset of their comments.
        /// Returns an empty list if an error occurs or if no posts match the criteria.
        /// </returns>
        /// <remarks>
        /// - Logs information about the pagination parameters and database operations.
        /// - Ensures predictable results by sorting posts and comments by their IDs or creation dates.
        /// - Logs any exceptions encountered during execution.
        /// </remarks>
        public async Task<List<Post>> GetAllPostsAsync(int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true)
        {
            try
            {
                if (pageNumber < 1)
                {
                    pageNumber = 1;
                    _logger.LogWarning("Invalid page number. Defaulting to 1.");
                }

                if (pageSize < 1)
                {
                    pageSize = 10;
                    _logger.LogWarning("Invalid page size. Defaulting to 10.");
                }

                if (commentPageNumber < 1)
                {
                    commentPageNumber = 1;
                    _logger.LogWarning("Invalid comment page number. Defaulting to 1.");
                }

                if (commentsPerPage < 1)
                {
                    commentsPerPage = 10;
                    _logger.LogWarning("Invalid number of comments per page. Defaulting to 10.");
                }

                _logger.LogInformation("Fetching posts from the database. Page: {Page}, Size: {Size}.",
                    pageNumber, pageSize);

                var query = _context.Posts.AsQueryable();

                if (includeComments)
                {
                    query = query.Include(p => p.Comments);
                }

                var posts = await query
                    .OrderBy(p => p.PostId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} posts. Total posts", posts.Count);

                if (includeComments)
                {
                    _logger.LogInformation("Fetching comments from the database. Page: {Page}, Size: {Size}.", pageNumber, pageSize);

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
                return posts;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching posts.");

                return new List<Post>();
            }
        }

        /// <summary>
        /// Retrieves a post by its unique identifier from the database. 
        /// Optionally includes associated comments based on the <paramref name="includeComments"/> parameter.
        /// </summary>
        /// <param name="postId">The unique identifier of the post to retrieve. Must be greater than 0.</param>
        /// <param name="includeComments">
        /// A boolean flag indicating whether to include the post's associated comments in the result. 
        /// Defaults to <c>true</c>.
        /// </param>
        /// <returns>
        /// The <see cref="Post"/> object corresponding to the provided <paramref name="postId"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="postId"/> is invalid.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if no post with the specified <paramref name="postId"/> is found.</exception>
        /// <exception cref="Exception">Thrown for any unexpected errors during database retrieval.</exception>
        public async Task<Post> GetPostByIdAsync(int postId, bool includeComments = true)
        {
            ValidatePost(postId);

            try
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

                if (!includeComments)
                {
                    post.Comments = new List<Comment>();
                }

                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the post with ID {postId}.", postId);
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
