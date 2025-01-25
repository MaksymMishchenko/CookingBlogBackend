using Microsoft.EntityFrameworkCore;
using PostApiService.Interfaces;
using PostApiService.Models;
using System.Data;

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
        /// Updates an existing post in the database. If the post with the given ID does not exist,
        /// a <see cref="KeyNotFoundException"/> is thrown. If the post is successfully updated, 
        /// the method returns <c>true</c>. If no changes are made, it returns <c>false</c>. 
        /// The method also handles database concurrency and update exceptions, logging them appropriately.
        /// </summary>
        /// <param name="post">The post object containing updated data to be saved in the database.</param>
        /// <returns>
        /// <c>true</c> if the post was successfully updated, otherwise <c>false</c> if no changes were made.
        /// </returns>
        /// <exception cref="KeyNotFoundException">Thrown if the post with the specified ID does not exist.</exception>
        /// <exception cref="DBConcurrencyException">Thrown if a concurrency issue occurs while updating the post.</exception>
        /// <exception cref="DbUpdateException">Thrown if the database update fails.</exception>
        /// <exception cref="Exception">Thrown if any unexpected error occurs during the update process.</exception>
        public async Task<bool> UpdatePostAsync(Post post)
        {
            var postExists = await _context.Posts
                .AsNoTracking()
                .AnyAsync(p => p.PostId == post.PostId);

            if (!postExists)
            {
                _logger.LogWarning("Post with ID {PostId} does not exist. Cannot edit.", post.PostId);
                throw new KeyNotFoundException($"Post with ID {post.PostId} does not exist.");
            }

            _context.Posts.Update(post);

            try
            {
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Successfully updated post with ID {PostId}.", post.PostId);
                    return true;
                }

                _logger.LogWarning("No changes were made to post with ID {PostId}.", post.PostId);
                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Database concurrency error occurred while updating the post with ID {PostId}." +
                    " This may be caused by conflicting changes in the database.", post.PostId);
                throw new DbUpdateConcurrencyException($"Database concurrency error occurred while updating the post with ID {post.PostId}." +
                    " This may be caused by conflicting changes in the database.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed for post with ID {PostId}.", post.PostId);
                throw new DbUpdateException($"Database update failed for post with ID {post.PostId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating post with ID {PostId}.", post.PostId);
                throw new Exception($"Unexpected error occurred while updating post with ID {post.PostId}.");
            }
        }

        /// <summary>
        /// Deletes a post with the given postId from the database.
        /// If the post does not exist, a KeyNotFoundException is thrown.
        /// If the deletion fails due to concurrency issues, a DbUpdateConcurrencyException is thrown.
        /// If a database-related error occurs during deletion, a DbUpdateException is thrown.
        /// If any unexpected error occurs, a generic Exception is thrown.
        /// </summary>
        /// <param name="postId">The ID of the post to be deleted.</param>
        /// <returns>True if the post was successfully deleted, false if no rows were affected.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the post with the specified ID does not exist.</exception>
        /// <exception cref="DbUpdateConcurrencyException">Thrown when a concurrency issue occurs while deleting the post.</exception>
        /// <exception cref="DbUpdateException">Thrown when a database error occurs during the deletion process.</exception>
        /// <exception cref="Exception">Thrown for any other unexpected errors during deletion.</exception>
        public async Task<bool> DeletePostAsync(int postId)
        {
            var existingPost = await _context.Posts.FindAsync(postId);

            if (existingPost == null)
            {
                _logger.LogWarning("Post with ID {PostId} does not exist. Deletion aborted.", postId);
                throw new KeyNotFoundException($"Post with ID {postId} does not exist.");
            }

            _context.Posts.Remove(existingPost);

            try
            {
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Post with ID {PostId} was successfully deleted.", postId);
                    return true;
                }

                _logger.LogWarning("No rows were affected when attempting to delete post with ID {PostId}.", postId);
                return false;
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogError(dbEx, "Concurrency issue occurred while deleting post with ID {PostId}.", postId);
                throw new DbUpdateConcurrencyException($"Database concurrency error occurred while deleting the post with ID {postId}." +
                    " This may be caused by conflicting changes in the database.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while deleting post with ID {PostId}.", postId);
                throw new DbUpdateException($"Database delete failed for post with ID {postId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting post with ID {PostId}.", postId);
                throw new Exception($"Unexpected error occurred while deleting post with ID {postId}.");
            }
        }
    }
}
