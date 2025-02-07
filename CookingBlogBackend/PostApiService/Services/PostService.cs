using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
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
        /// Retrieves a paginated list of posts from the database with optional comments pagination.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (starting from 1).</param>
        /// <param name="pageSize">The number of posts to retrieve per page.</param>
        /// <param name="commentPageNumber">The page number to retrieve comments for each post (starting from 1).</param>
        /// <param name="commentsPerPage">The number of comments to retrieve per page for each post.</param>
        /// <param name="includeComments">Indicates whether to include comments in the response.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of posts.</returns>        
        public async Task<List<Post>> GetAllPostsAsync(int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Posts.AsNoTracking();

            if (includeComments)
            {
                query = query.Include(p => p.Comments);
            }

            var posts = await query
                .OrderBy(p => p.PostId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            ProcessComments(posts, includeComments, commentPageNumber, commentsPerPage);

            return posts;

            void ProcessComments(List<Post> posts, bool includeComments, int commentPageNumber, int commentsPerPage)
            {
                if (includeComments)
                {
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
        /// Retrieves a post by its ID from the database, with optional inclusion of comments.
        /// </summary>
        /// <param name="postId">The ID of the post to retrieve.</param>
        /// <param name="includeComments">A boolean flag indicating whether to include comments for the post. Default is <c>true</c>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the post if found.</returns>
        /// <exception cref="PostNotFoundException">Thrown when a post with the specified ID is not found in the database.</exception>
        public async Task<Post> GetPostByIdAsync(int postId, bool includeComments = true)
        {
            var query = _context.Posts.AsNoTracking();

            if (includeComments)
            {
                query = query.Include(p => p.Comments);
            }

            var post = await query.FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null)
            {
                throw new PostNotFoundException(postId);
            }

            return post;
        }

        /// <summary>
        /// Adds a new post to the database. If a post with the same title already exists, an exception is thrown.
        /// If the post is successfully added, the method returns the created post object.
        /// </summary>
        /// <param name="post">The post object to be added to the database.</param>
        /// <returns>The added post object, including its generated properties such as PostId.</returns>
        /// <exception cref="PostAlreadyExistException">Thrown if a post with the same title already exists in the database.</exception>
        /// <exception cref="PostNotSavedException">Thrown if the post could not be saved to the database.</exception>
        public async Task<Post> AddPostAsync(Post post)
        {
            var existingPost = await _context.Posts
                .AsNoTracking()
                .AnyAsync(p => p.Title == post.Title);

            if (existingPost)
            {                
                throw new PostAlreadyExistException(post.Title);
            }

            await _context.Posts.AddAsync(post);
            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {                
                throw new PostNotSavedException(post.Title);
            }

            return post;
        }

        /// <summary>
        /// Updates an existing post in the database.
        /// </summary>
        /// <param name="post">The post object containing updated data.</param>
        /// <returns>Returns <c>true</c> if the post was successfully updated, otherwise throws an exception.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the specified post ID does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no changes were made to the post.</exception>
        /// <exception cref="DbUpdateException">Thrown when a database update error occurs.</exception>
        /// <exception cref="SqlException">Thrown when an SQL-related error occurs during the update.</exception>
        /// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
        public async Task UpdatePostAsync(Post post)
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

                if (result <= 0)
                {
                    _logger.LogWarning("No changes were made to post with ID {PostId}.", post.PostId);
                    throw new InvalidOperationException($"No changes were made to post with ID {post.PostId}.");
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating post.");
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while updating post.");
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
                    _logger.LogWarning("No rows were affected when attempting to delete post with ID {PostId}.", postId);
                    throw new InvalidOperationException($"Failed to delete post with ID {postId}. No changes were made.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while deleting post with ID {PostId}.", postId);
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while deleting post.");
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
