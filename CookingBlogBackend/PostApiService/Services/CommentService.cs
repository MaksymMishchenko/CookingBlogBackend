using PostApiService.Helper;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository _postRepository;
        private readonly IWebContext _webContext;

        public CommentService(ICommentRepository commentRepository,
            IPostRepository postRepository,
            IWebContext webContext)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _webContext = webContext;
        }

        /// <summary>
        /// Adds a new comment to a post with the given post ID.
        /// </summary>        
        public async Task<Result<CommentCreatedDto>> AddCommentAsync(int postId, string content, CancellationToken ct = default)
        {
            var userId = _webContext.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<CommentCreatedDto>.Unauthorized(Auth.LoginM.Errors.UnauthorizedAccess,
                    Auth.LoginM.Errors.UnauthorizedAccessCode);
            }

            var postExists = await _postRepository.IsAvailableForCommentingAsync(postId, ct);

            if (!postExists)
            {
                Log.Warning(Posts.NotFound, postId);
                return Result<CommentCreatedDto>.NotFound(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            var comment = new Comment
            {
                Content = content,
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepository.AddAsync(comment, ct);
            await _commentRepository.SaveChangesAsync(ct);

            var commentDto = comment.ToCreatedDto(_webContext.UserName);

            return Result<CommentCreatedDto>.Success(commentDto, CommentM.Success.CommentAddedSuccessfully);
        }

        /// <summary>
        /// Updates the content of an existing comment after verifying ownership.
        /// </summary>             
        public async Task<Result<CommentUpdatedDto>> UpdateCommentAsync(int commentId, string content, CancellationToken ct = default)
        {
            var userId = _webContext.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<CommentUpdatedDto>.Unauthorized(Auth.LoginM.Errors.UnauthorizedAccess,
                    Auth.LoginM.Errors.UnauthorizedAccessCode);
            }

            var existingComment = await _commentRepository.GetWithUserAsync(commentId, ct);

            if (existingComment == null)
            {
                Log.Warning(Comments.NotFound, commentId);
                return Result<CommentUpdatedDto>.NotFound(CommentM.Errors.NotFound, CommentM.Errors.NotFoundCode);
            }

            var isAdmin = _webContext.IsAdmin;

            if (existingComment.UserId != userId && !isAdmin)
            {
                Log.Warning(Security.AccessDenied, userId, "Update", "Comment", commentId, existingComment.UserId, _webContext.IpAddress
                );

                return Result<CommentUpdatedDto>.Forbidden(CommentM.Errors.AccessDenied, CommentM.Errors.AccessDeniedCode);
            }

            var authorName = existingComment.User.UserName ?? UnknownUser;

            existingComment.Content = content;
            if (isAdmin && existingComment.UserId != userId)
            {
                existingComment.IsEditedByAdmin = true;

                Log.Information(Comments.AdminUpdatedComment,
                     userId,
                     commentId,
                     _webContext.UserName);
            }

            await _commentRepository.SaveChangesAsync(ct);
            var commentDto = existingComment.ToUpdatedDto(authorName);

            return Result<CommentUpdatedDto>.Success(commentDto, CommentM.Success.CommentUpdatedSuccessfully);
        }

        /// <summary>
        /// Deletes a comment by its ID after verifying ownership.
        /// </summary>       
        public async Task<Result> DeleteCommentAsync(int commentId, CancellationToken ct = default)
        {
            var userId = _webContext.UserId;

            if (string.IsNullOrEmpty(userId))
            {                
                return Result.Unauthorized(Auth.LoginM.Errors.UnauthorizedAccess,
                    Auth.LoginM.Errors.UnauthorizedAccessCode);
            }

            var existingComment = await _commentRepository.GetByIdAsync(commentId, ct);

            if (existingComment == null)
            {
                Log.Warning(Comments.NotFound, commentId);
                return Result.NotFound(CommentM.Errors.NotFound, CommentM.Errors.NotFoundCode);
            }

            var isAdmin = _webContext.IsAdmin;
            var ip = _webContext.IpAddress;

            if (existingComment.UserId != userId && !isAdmin)
            {
                Log.Warning(Security.AccessDenied, userId, "Delete", "Comment", commentId, existingComment.UserId, ip);

                return Result.Forbidden(CommentM.Errors.AccessDenied, CommentM.Errors.AccessDeniedCode);
            }

            await _commentRepository.DeleteAsync(existingComment, ct);
            await _commentRepository.SaveChangesAsync(ct);

            if (isAdmin && existingComment.UserId != userId)
            {
                Log.Information(Comments.AdminDeletedComment, userId, commentId, existingComment.UserId, ip);
            }

            return Result.Success(CommentM.Success.CommentDeletedSuccessfully);
        }
    }
}
