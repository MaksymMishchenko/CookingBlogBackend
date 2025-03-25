using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Services
{
    public class CommentService : ICommentService
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthService _authService;

        public CommentService(IApplicationDbContext context,
            IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        /// <summary>
        /// Adds a new comment to a post with the given post ID. If the post does not exist, an exception is thrown. 
        /// The method also handles various exceptions that may occur during the database operation and logs them appropriately.
        /// </summary>
        /// <param name="postId">The ID of the post to which the comment will be added.</param>
        /// <param name="comment">The comment object containing the details of the comment to be added.</param>
        /// <exception cref="PostNotFoundException">Thrown if the post with the specified ID does not exist.</exception>
        /// <exception cref="AddCommentFailedException">Thrown if the comment could not be added to the database.</exception>        
        public async Task AddCommentAsync(int postId, Comment comment)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId);
            if (!postExists)
            {
                throw new PostNotFoundException(postId);
            }

            var user = await _authService.GetCurrentUserAsync();

            comment.PostId = postId;
            comment.UserId = user.Id;
            await _context.Comments.AddAsync(comment);

            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                throw new AddCommentFailedException(postId);
            }
        }

        /// <summary>
        /// Updates the content of a comment with the specified comment ID.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be updated.</param>
        /// <param name="comment">An instance of <see cref="EditCommentModel"/> containing the new content for the comment.</param>
        /// <exception cref="CommentNotFoundException">Thrown when a comment with the specified ID is not found.</exception>
        /// <exception cref="UpdateCommentFailedException">Thrown when the update operation fails for reasons unrelated to business logic.</exception>
        /// <returns>A task that represents the asynchronous operation.</returns>      
        public async Task UpdateCommentAsync(int commentId, EditCommentModel comment)
        {
            var existingComment = await _context.Comments.FindAsync(commentId);

            if (existingComment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            existingComment.Content = comment.Content;

            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                throw new UpdateCommentFailedException(commentId);
            }
        }

        /// <summary>
        /// Asynchronously deletes a comment identified by the specified comment ID.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be deleted.</param>
        /// <exception cref="CommentNotFoundException">
        /// Thrown when no comment is found with the specified ID.
        /// </exception>
        /// <exception cref="DeletePostFailedException">
        /// Thrown when the deletion operation fails.
        /// </exception>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        public async Task DeleteCommentAsync(int commentId)
        {
            var existingComment = await _context.Comments.FindAsync(commentId);

            if (existingComment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            _context.Comments.Remove(existingComment);

            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                throw new DeleteCommentFailedException(commentId);
            }
        }
    }
}
