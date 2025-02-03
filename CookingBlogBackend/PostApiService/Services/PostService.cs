using Microsoft.Data.SqlClient;
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
        /// Asynchronously retrieves a paginated list of posts from the database, optionally including comments.
        /// This method supports pagination for both posts and comments, and allows filtering of comments based on the provided parameters.
        /// The method also handles logging for various operations and error scenarios.
        /// </summary>
        /// <param name="pageNumber">The page number for the posts to be retrieved. Defaults to 1.</param>
        /// <param name="pageSize">The number of posts per page. Defaults to 10.</param>
        /// <param name="commentPageNumber">The page number for the comments to be retrieved. Defaults to 1.</param>
        /// <param name="commentsPerPage">The number of comments per page. Defaults to 10.</param>
        /// <param name="includeComments">A flag to determine whether to include comments with the posts. Defaults to true.</param>
        /// <param name="cancellationToken">A token to cancel the operation, if requested. Defaults to default.</param>
        /// <returns>A list of posts with optional comments, based on the provided pagination and inclusion parameters.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
        /// <exception cref="SqlException">Thrown when a database error occurs while fetching posts.</exception>
        /// <exception cref="Exception">Thrown for any unexpected errors during the process.</exception>
        public async Task<List<Post>> GetAllPostsAsync(int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true,
            CancellationToken cancellationToken = default)
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
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Fetched {Count} posts. Total posts", posts.Count);

                ProcessComments(posts, includeComments, commentPageNumber, commentsPerPage);

                return posts;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "The request was cancelled.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "The request timed out.");
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error while fetching posts.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching posts.");
                throw;
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
        /// Asynchronously retrieves a post by its ID from the database, optionally including its comments.
        /// If the post is not found, a <see cref="KeyNotFoundException"/> is thrown.
        /// The method handles different exceptions related to operation cancellation, timeouts, and database errors,
        /// logging each occurrence appropriately.
        /// </summary>
        /// <param name="postId">The ID of the post to be retrieved.</param>
        /// <param name="includeComments">A flag to determine whether to include comments for the post. Defaults to true.</param>
        /// <returns>The post with the specified ID, including comments if requested, or <c>null</c> if not found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the post with the specified ID is not found.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
        /// <exception cref="SqlException">Thrown when a database error occurs while fetching the post.</exception>
        /// <exception cref="Exception">Thrown for any unexpected errors during the process.</exception>
        public async Task<Post> GetPostByIdAsync(int postId, bool includeComments = true)
        {
            var query = _context.Posts.AsNoTracking();

            if (includeComments)
            {
                query = query.Include(p => p.Comments);
            }

            try
            {
                var post = await query.FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null)
                {
                    _logger.LogWarning("Post with ID {postId} not found.", postId);
                    throw new KeyNotFoundException($"Post with ID {postId} was not found.");
                }

                return post;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "The request was cancelled.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "The request timed out.");
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error while fetching post.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching post.");
                throw;
            }
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
        public async Task<Post> AddPostAsync(Post post)
        {
            var existingPost = await _context.Posts
            .AnyAsync(p => p.Title == post.Title);

            if (existingPost)
            {
                _logger.LogWarning("A post with the title '{Title}' already exists.", post.Title);
                return null;
            }

            await _context.Posts.AddAsync(post);

            try
            {
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Post was added successfully.");
                    return post;
                }
                _logger.LogWarning($"Failed to add post with title: {post.Title}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "An unexpected error occurred while adding post to database");

                throw new Exception("An unexpected error occurred while adding post to database.");
            }
        }

        /// <summary>
        /// Updates an existing post in the database.
        /// </summary>
        /// <param name="post">The post entity with updated values.</param>
        /// <returns>True if the update is successful; otherwise, an exception is thrown.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the post with the specified ID does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no changes were made to the post.</exception>
        /// <exception cref="DbUpdateConcurrencyException">Thrown when a concurrency conflict occurs while saving changes.</exception>
        /// <exception cref="DbUpdateException">Thrown when the database update fails due to an error.</exception>
        /// <exception cref="Exception">Thrown for any unexpected errors.</exception>
        public async Task<bool> UpdatePostAsync(Post post)
        {
            var existingPost = await _context.Posts
                .FindAsync(post.PostId);

            if (existingPost == null)
            {
                _logger.LogWarning("Post with ID {PostId} does not exist. Cannot edit.", post.PostId);
                throw new KeyNotFoundException($"Post with ID {post.PostId} not found. Please check the Post ID.");
            }

            existingPost.Title = post.Title;
            existingPost.Description = post.Description;
            existingPost.Content = post.Content;
            existingPost.ImageUrl = post.ImageUrl;
            existingPost.MetaTitle = post.MetaTitle;
            existingPost.MetaDescription = post.MetaDescription;
            existingPost.Slug = post.Slug;

            try
            {
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Successfully updated post with ID {PostId}.", post.PostId);
                    return true;
                }

                _logger.LogWarning("No changes were made to post with ID {PostId}.", post.PostId);
                throw new InvalidOperationException($"No changes were made to post with ID {post.PostId}.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Database concurrency error occurred while updating the post with ID {PostId}.", post.PostId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed for post with ID {PostId}.", post.PostId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating post with ID {PostId}.", post.PostId);
                throw;
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
        public async Task DeletePostAsync(int postId)
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

                if (result <= 0)
                {
                    _logger.LogWarning("No rows were affected when attempting to delete post with ID {PostId}.");
                    throw new InvalidOperationException($"Failed to delete post with ID {postId}. No changes were made.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while deleting post with ID {PostId}.", postId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting post with ID {PostId}.", postId);
                throw;
            }
        }
    }
}
