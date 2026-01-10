using PostApiService.Exceptions;
using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using System.Data.Common;

namespace PostApiService.Services
{
    public class CommentService : ICommentService
    {
        private readonly IRepository<Comment> _commentRepository;
        private readonly IRepository<Post> _postRepository;
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommentService(IRepository<Comment> commentRepository,
            IRepository<Post> postRepository,
            IAuthService authService,
            IHttpContextAccessor httpContextAccessor)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _authService = authService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Adds a new comment to a post with the given post ID.
        /// </summary>        
        public async Task<Result<CommentDto>> AddCommentAsync
            (int postId, string content, CancellationToken ct = default)
        {
            var postExists = await _postRepository.AnyAsync(p => p.Id == postId, ct);

            if (!postExists)
            {
                Log.Warning(Posts.NotFound, postId);
                return Result<CommentDto>.NotFound(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            var user = await _authService.GetCurrentUserAsync();

            var comment = new Comment
            {
                Content = content,
                PostId = postId,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepository.AddAsync(comment, ct);
            await _commentRepository.SaveChangesAsync(ct);

            var commentDto = comment.ToDto(user.UserName!);

            return Result<CommentDto>.Success(commentDto, CommentM.Success.CommentAddedSuccessfully);
        }

        /// <summary>
        /// Updates the content of an existing comment after verifying ownership.
        /// </summary>             
        public async Task<Result<CommentDto>> UpdateCommentAsync(int commentId, string content, CancellationToken ct = default)
        {
            var existingComment = await _commentRepository.GetByIdAsync(commentId, ct);

            if (existingComment == null)
            {
                Log.Warning(Comments.NotFound, commentId);
                return Result<CommentDto>.NotFound(CommentM.Errors.NotFound, CommentM.Errors.NotFoundCode);
            }

            var user = await _authService.GetCurrentUserAsync();

            if (existingComment.UserId != user.Id)
            {
                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

                Log.Warning(Security.AccessDenied, user.Id, "Update", "Comment", commentId, existingComment.UserId, ip
                );

                return Result<CommentDto>.Forbidden(CommentM.Errors.AccessDenied, CommentM.Errors.AccessDeniedCode);
            }

            existingComment.Content = content;
            await _commentRepository.SaveChangesAsync(ct);

            var commentDto = existingComment.ToDto(user.UserName!);

            return Result<CommentDto>.Success(commentDto, CommentM.Success.CommentUpdatedSuccessfully);
        }

        /// <summary>
        /// Asynchronously deletes a comment identified by the specified comment ID.
        /// </summary>        
        public async Task DeleteCommentAsync(int commentId, CancellationToken ct = default)
        {
            var existingComment = await _commentRepository.GetByIdAsync(commentId, ct);

            //if (existingComment == null)
            //{
            //    throw new CommentNotFoundException(commentId);
            //}

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
