using PostApiService.Helper;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class CommentService : BaseService, ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IHtmlSanitizationService _sanitizer;
        private readonly IPostRepository _postRepository;

        public CommentService(ICommentRepository commentRepository,
             IHtmlSanitizationService sanitizer,
            IPostRepository postRepository,
            IWebContext webContext) : base(webContext)
        {
            _commentRepository = commentRepository;
            _sanitizer = sanitizer;
            _postRepository = postRepository;
        }

        /// <summary>
        /// Adds a new comment to a post with the given post ID.
        /// </summary>        
        public async Task<Result<CommentCreatedDto>> AddCommentAsync
            (int postId, string content, CancellationToken ct = default)
        {
            var userId = WebContext.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized<CommentCreatedDto>();
            }

            var sanitizedContent = _sanitizer.SanitizeComment(content);

            if (!string.Equals(content, sanitizedContent, StringComparison.Ordinal))
            {
                var traceContent = content.Truncate(500);
                Log.Warning(Security.XssDetectedOnCommentCreate, postId, userId, WebContext.IpAddress, traceContent);
            }

            if (string.IsNullOrWhiteSpace(sanitizedContent))
            {
                return Invalid<CommentCreatedDto>(CommentM.Errors.Empty, CommentM.Errors.EmptyCode);
            }

            var postExists = await _postRepository.IsAvailableForCommentingAsync(postId, ct);

            if (!postExists)
            {
                Log.Warning(Posts.NotFound, postId);

                return NotFound<CommentCreatedDto>(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            var comment = new Comment
            {
                Content = sanitizedContent,
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepository.AddAsync(comment, ct);
            await _commentRepository.SaveChangesAsync(ct);

            var commentDto = comment.ToCreatedDto(WebContext.UserName);

            return Success(commentDto, CommentM.Success.CommentAddedSuccessfully);
        }

        /// <summary>
        /// Updates the content of an existing comment after verifying ownership.
        /// </summary>             
        public async Task<Result<CommentUpdatedDto>> UpdateCommentAsync(int commentId, string content, CancellationToken ct = default)
        {
            var userId = WebContext.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized<CommentUpdatedDto>();
            }

            var sanitizedContent = _sanitizer.SanitizeComment(content);

            if (!string.Equals(content, sanitizedContent, StringComparison.Ordinal))
            {
                var traceContent = content.Truncate(500);
                Log.Warning(Security.XssDetectedOnCommentUpdate, commentId, userId, WebContext.IpAddress, traceContent);
            }

            if (string.IsNullOrWhiteSpace(sanitizedContent))
            {
                return Invalid<CommentUpdatedDto>(CommentM.Errors.Empty, CommentM.Errors.EmptyCode);
            }

            var existingComment = await _commentRepository.GetWithUserAsync(commentId, ct);

            if (existingComment == null)
            {
                Log.Warning(Comments.NotFound, commentId);

                return NotFound<CommentUpdatedDto>(CommentM.Errors.NotFound, CommentM.Errors.NotFoundCode);
            }

            var isAdmin = WebContext.IsAdmin;

            if (existingComment.UserId != userId && !isAdmin)
            {
                Log.Warning(Security.AccessDenied, userId, "Update", "Comment", commentId, existingComment.UserId, WebContext.IpAddress
                );

                return Forbidden<CommentUpdatedDto>(CommentM.Errors.AccessDenied, CommentM.Errors.AccessDeniedCode);
            }

            var authorName = existingComment.User.UserName ?? UnknownUser;

            existingComment.Content = sanitizedContent;
            if (isAdmin && existingComment.UserId != userId)
            {
                existingComment.IsEditedByAdmin = true;

                Log.Information(Comments.AdminUpdatedComment,
                     userId,
                     commentId,
                     WebContext.UserName);
            }

            await _commentRepository.SaveChangesAsync(ct);

            var commentDto = existingComment.ToUpdatedDto(authorName);

            return Success(commentDto, CommentM.Success.CommentUpdatedSuccessfully);
        }

        /// <summary>
        /// Deletes a comment by its ID after verifying ownership.
        /// </summary>       
        public async Task<Result> DeleteCommentAsync(int commentId, CancellationToken ct = default)
        {
            var userId = WebContext.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var existingComment = await _commentRepository.GetByIdAsync(commentId, ct);

            if (existingComment == null)
            {
                Log.Warning(Comments.NotFound, commentId);

                return NotFound(CommentM.Errors.NotFound, CommentM.Errors.NotFoundCode);
            }

            var isAdmin = WebContext.IsAdmin;
            var ip = WebContext.IpAddress;

            if (existingComment.UserId != userId && !isAdmin)
            {
                Log.Warning(Security.AccessDenied, userId, "Delete", "Comment", commentId, existingComment.UserId, ip);

                return Forbidden(CommentM.Errors.AccessDenied, CommentM.Errors.AccessDeniedCode);
            }

            await _commentRepository.DeleteAsync(existingComment, ct);
            await _commentRepository.SaveChangesAsync(ct);

            if (isAdmin && existingComment.UserId != userId)
            {
                Log.Information(Comments.AdminDeletedComment, userId, commentId, existingComment.UserId, ip);
            }

            return Success(CommentM.Success.CommentDeletedSuccessfully);
        }
    }
}
