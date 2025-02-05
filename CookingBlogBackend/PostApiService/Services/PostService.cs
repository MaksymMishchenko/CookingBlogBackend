﻿using Microsoft.Data.SqlClient;
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
        /// Adds a new post to the database. If a post with the same title already exists, it returns null.
        /// If the post is successfully added, the post object is returned. If the addition fails or an exception occurs,
        /// an appropriate error is logged and an exception is thrown.
        /// </summary>
        /// <param name="post">The post to be added.</param>
        /// <returns>
        /// A Task representing the asynchronous operation, with a <see cref="Post"/> result if the post is successfully added,
        /// or null if the post with the same title already exists. 
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the post could not be added to the database.</exception>
        /// <exception cref="DbUpdateException">Thrown when there is a database update error.</exception>
        /// <exception cref="SqlException">Thrown when there is an error with the SQL server.</exception>
        /// <exception cref="Exception">Thrown for any unexpected errors that occur during the operation.</exception>
        public async Task<Post> AddPostAsync(Post post)
        {
            var existingPost = await _context.Posts
                .AsNoTracking()
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

                if (result <= 0)
                {
                    _logger.LogWarning($"Failed to add post with title: {post.Title}");

                    throw new InvalidOperationException
                        ("Failed to add the post to the database.");
                }

                return post;

            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while adding post.");
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while adding post.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "An unexpected error occurred while adding post to database.");
                throw;
            }
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
