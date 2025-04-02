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
        /// Adds a new comment to a post with the given post ID.
        /// </summary>        
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
