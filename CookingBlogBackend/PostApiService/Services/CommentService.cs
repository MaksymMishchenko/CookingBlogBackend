using Microsoft.EntityFrameworkCore;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Services
{
    /// <summary>
    /// Service class for managing comments on posts. This class provides methods to add, edit, and delete comments.
    /// </summary>
    public class CommentService : ICommentService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CommentService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentService"/> class with the specified database context and logger.
        /// </summary>
        /// <param name="context">The database context used to interact with the database tables related to comments.</param>
        /// <param name="logger">The logger used to log operations and events related to comments.</param>
        public CommentService(IApplicationDbContext context, ILogger<CommentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adds a new comment to a specified post.
        /// </summary>
        /// <param name="postId">The ID of the post to which the comment will be added.</param>
        /// <param name="comment">The comment object to be added.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains a boolean indicating whether the comment was added successfully.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the specified post does not exist in the database.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when an unexpected error occurs during the database save operation.
        /// </exception>
        public async Task<bool> AddCommentAsync(int postId, Comment comment)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId);
            if (!postExists)
            {
                _logger.LogWarning($"Post with ID {postId} does not exist. Cannot add comment");

                throw new KeyNotFoundException($"Post with ID {postId} does not exist.");
            }

            comment.PostId = postId;            
            await _context.Comments.AddAsync(comment);

            try
            {
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation($"Comment was added successfully to post id: {postId}");

                    return true;
                }

                _logger.LogWarning($"Failed to add comment to post id: {postId}");

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "An unexpected error occurred while saving comment to post ID: {postId}", postId);

                throw new Exception("An unexpected error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Updates the content of an existing comment by its ID.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be updated.</param>
        /// <param name="comment">The model containing the new content for the comment.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains a boolean indicating whether the comment was successfully updated.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the specified comment does not exist in the database.
        /// </exception>
        /// <exception cref="DbUpdateConcurrencyException">
        /// Thrown when a concurrency issue occurs during the update process, such as conflicting changes.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when an unexpected error occurs during the database update operation.
        /// </exception>
        public async Task<bool> UpdateCommentAsync(int commentId, EditCommentModel comment)
        {
            var existingComment = await _context.Comments.FindAsync(commentId);

            if (existingComment == null)
            {
                _logger.LogWarning("Comment with ID {CommentId} does not exist. Cannot edit.", commentId);

                throw new InvalidOperationException($"Comment with ID {commentId} does not exist");
            }

            existingComment.Content = comment.Content;

            try
            {
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Comment with ID {CommentId} was successfully updated.", commentId);

                    return true;
                }

                _logger.LogWarning("No rows were affected while updating comment with ID {CommentId}.", commentId);

                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency issue while updating comment ID {CommentId}.", commentId);

                throw new DbUpdateConcurrencyException($"Concurrency issue while updating comment ID {commentId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating comment ID {CommentId}.", commentId);

                throw new Exception("An unexpected error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Deletes a comment by its ID from the database.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be deleted.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the specified comment does not exist in the database.
        /// </exception>
        /// <exception cref="DbUpdateConcurrencyException">
        /// Thrown when a concurrency issue occurs while deleting the comment.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when an unexpected error occurs during the delete operation.
        /// </exception>
        public async Task DeleteCommentAsync(int commentId)
        {
            var existingComment = await _context.Comments.FindAsync(commentId);

            if (existingComment == null)
            {
                _logger.LogWarning("Comment with ID {CommentId} does not exist. Cannot delete.", commentId);

                throw new KeyNotFoundException($"Comment with ID {commentId} does not exist");
            }

            _context.Comments.Remove(existingComment);

            try
            {
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("Comment with ID {CommentId} removed successfully.", commentId);

            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency issue while removing comment ID {CommentId}.", commentId);

                throw new DbUpdateConcurrencyException($"Concurrency issue while removing comment ID {commentId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while removing comment ID {CommentId}.", commentId);

                throw new Exception("An unexpected error occurred. Please try again later.");
            }
        }
    }
}
