using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using System.Data.Common;

namespace PostApiService.Services
{
    public class CommentService : ICommentService
    {
        private readonly IRepository<Comment> _commentRepository;
        private readonly IRepository<Post> _postRepository;
        private readonly IAuthService _authService;

        public CommentService(IRepository<Comment> commentRepository,
            IRepository<Post> postRepository,
            IAuthService authService)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _authService = authService;
        }

        /// <summary>
        /// Adds a new comment to a post with the given post ID.
        /// </summary>        
        public async Task AddCommentAsync(int postId, Comment comment, CancellationToken ct = default)
        {
            var postExists = await _postRepository.AnyAsync(p => p.Id == postId, ct);
            if (!postExists)
            {
                throw new PostNotFoundException(postId);
            }

            var user = await _authService.GetCurrentUserAsync();

            comment.PostId = postId;
            comment.UserId = user.Id;

            try
            {
                await _commentRepository.AddAsync(comment, ct);
                await _commentRepository.SaveChangesAsync(ct);
            }
            catch (DbException ex)
            {
                throw new AddCommentFailedException(postId, ex);
            }
        }

        /// <summary>
        /// Updates the content of a comment with the specified comment ID.
        /// </summary>              
        public async Task UpdateCommentAsync(int commentId, EditCommentModel comment, CancellationToken ct = default)
        {
            var existingComment = await _commentRepository.GetByIdAsync(commentId, ct);

            if (existingComment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            existingComment.Content = comment.Content;

            try
            {
                await _commentRepository.UpdateAsync(existingComment, ct);
                await _commentRepository.SaveChangesAsync(ct);
            }
            catch (DbException ex)
            {
                throw new UpdateCommentFailedException(commentId, ex);
            }
        }

        /// <summary>
        /// Asynchronously deletes a comment identified by the specified comment ID.
        /// </summary>        
        public async Task DeleteCommentAsync(int commentId, CancellationToken ct = default)
        {
            var existingComment = await _commentRepository.GetByIdAsync(commentId, ct);

            if (existingComment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            try
            {
                await _commentRepository.DeleteAsync(existingComment, ct);
                await _commentRepository.SaveChangesAsync(ct);
            }
            catch (DbException ex)
            {
                throw new DeleteCommentFailedException(commentId, ex);
            }
        }
    }
}
