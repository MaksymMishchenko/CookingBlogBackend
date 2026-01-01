using PostApiService.Controllers.Filters;
using PostApiService.Interfaces;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    [Authorize(Policy = TS.Policies.ContributorPolicy)]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        /// <summary>
        /// Adds a comment to a specific post.
        /// </summary>        
        [HttpPost("{postId}")]
        [ValidateId]
        [ValidateModel]
        public async Task<IActionResult> AddCommentAsync
            (int postId, [FromBody] Comment comment, CancellationToken ct = default)
        {
            await _commentService.AddCommentAsync(postId, comment, ct);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentM.Success.CommentAddedSuccessfully));
        }

        /// <summary>
        /// Updates an existing comment based on the provided comment ID.
        /// </summary>        
        [HttpPut("{commentId}")]
        [ValidateId]
        [ValidateModel]
        public async Task<IActionResult> UpdateCommentAsync
            (int commentId, [FromBody] EditCommentModel comment, CancellationToken ct = default)
        {
            await _commentService.UpdateCommentAsync(commentId, comment, ct);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentM.Success.CommentUpdatedSuccessfully));
        }

        /// <summary>
        /// Deletes a comment by its ID.
        /// </summary>        
        [HttpDelete("{commentId}")]
        [ValidateId]
        public async Task<IActionResult> DeleteCommentAsync
            (int commentId, CancellationToken ct = default)
        {
            await _commentService.DeleteCommentAsync(commentId, ct);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentM.Success.CommentDeletedSuccessfully));
        }
    }
}
