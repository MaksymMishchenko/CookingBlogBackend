﻿using Microsoft.EntityFrameworkCore;
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
            comment.CreatedAt = DateTime.UtcNow;
            _context.Comments.Add(comment);

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
        /// Deletes a comment by its ID.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be deleted.</param>
        /// <returns>True if the comment was successfully deleted, otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="commentId"/> is less than or equal to zero.</exception>
        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            ValidateComment(commentId);

            try
            {
                var commentExist = await _context.Comments.FindAsync(commentId);
                if (commentExist == null)
                {
                    _logger.LogWarning($"Comment with id: {commentId} does not exist", commentId);
                    return false;
                }

                _context.Comments.Remove(commentExist);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Comment with ID {CommentId} was successfully deleted.", commentId);
                    return true;
                }

                _logger.LogWarning("No rows were affected when attempting to delete comment with ID {CommentId}.", commentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occured while removing comment with ID {commentId}", commentId);
                throw;
            }
        }

        /// <summary>
        /// Edits an existing comment.
        /// </summary>
        /// <param name="comment">The comment object containing the updated content.</param>
        /// <returns>True if the comment was successfully updated, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="comment"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="comment.Content"/> is null or empty, or if the <paramref name="comment.CommentId"/> is invalid.</exception>
        /// <exception cref="DbUpdateConcurrencyException">Thrown if a concurrency conflict occurs during the update.</exception>
        public async Task<bool> EditCommentAsync(Comment comment)
        {
            ValidateComment(comment);

            try
            {
                var commentExists = await _context.Comments
                    .AsNoTracking()
                    .AnyAsync(c => c.CommentId == comment.CommentId);
                if (!commentExists)
                {
                    _logger.LogWarning("Comment with ID {CommentId} does not exist. Cannot edit.", comment.CommentId);
                    return false;
                }

                _context.Comments.Attach(comment);
                //_context.Entry(comment).Property(c => c.Content).IsModified = true;
                //_context.Entry(comment).Property(c => c.PostId).IsModified = true;

                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Comment with ID {CommentId} was successfully updated.", comment.CommentId);
                    return true;
                }

                _logger.LogWarning("No rows were affected while updating comment with ID {CommentId}.", comment.CommentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while editing comment with ID {CommentId}", comment.CommentId);
                throw;
            }
        }

        /// <summary>
        /// Validates the provided post ID and the associated comment.
        /// Ensures the post ID is greater than zero and the comment is valid.
        /// </summary>
        /// <param name="postId">The ID of the post to validate.</param>
        /// <param name="comment">The comment to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the post ID is less than or equal to zero.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the comment is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the comment fails validation (e.g., invalid content or ID).</exception>
        private void ValidateComment(int postId, Comment comment)
        {
            if (postId <= 0)
            {
                _logger.LogError($"Invalid post ID: {postId}");
                throw new ArgumentException("Post ID must be greater than zero.", nameof(postId));
            }

            ValidateCommentObject(comment);
        }

        /// <summary>
        /// Validates the provided comment ID to ensure it is greater than zero.
        /// </summary>
        /// <param name="commentId">The ID of the comment to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the comment ID is less than or equal to zero.</exception>
        private void ValidateComment(int commentId)
        {
            if (commentId <= 0)
            {
                _logger.LogError($"Invalid comment ID: {commentId}");
                throw new ArgumentException($"Post ID must be greater than zero.", nameof(commentId));
            }
        }

        /// <summary>
        /// Validates the provided <see cref="Comment"/> object to ensure it meets the required criteria.
        /// </summary>
        /// <param name="comment">The <see cref="Comment"/> object to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="comment"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="comment.Content"/> is null, empty, or whitespace,
        /// or if the <paramref name="comment.CommentId"/> is less than or equal to zero.
        private void ValidateComment(Comment comment)
        {
            ValidateCommentObject(comment);

            if (string.IsNullOrWhiteSpace(comment.Content))
            {
                _logger.LogError("Comment content cannot be null or empty.");
                throw new ArgumentException("Comment content cannot be null or empty.", nameof(comment.Content));
            }

            if (comment.CommentId <= 0)
            {
                _logger.LogError("Invalid comment ID: {CommentId}", comment.CommentId);
                throw new ArgumentException("Invalid comment ID.", nameof(comment.CommentId));
            }
        }

        /// <summary>
        /// Validates the provided <see cref="Comment"/> object to ensure it is not null.
        /// </summary>
        /// <param name="comment">The <see cref="Comment"/> object to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="comment"/> is null.</exception>
        private void ValidateCommentObject(Comment comment)
        {
            if (comment == null)
            {
                _logger.LogError($"Attempted to edit a null comment");
                throw new ArgumentNullException(nameof(comment), $"Comment cannot be null");
            }
        }
    }
}
