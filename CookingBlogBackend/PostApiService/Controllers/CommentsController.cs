using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
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
        public async Task<IActionResult> AddCommentAsync
            (int postId, [FromBody] CommentCreateDto comment, CancellationToken ct = default)
        {
            var result = await _commentService.AddCommentAsync(postId, comment.Content, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Updates an existing comment based on the provided comment ID.
        /// </summary>        
        [HttpPut("{commentId}")]
        public async Task<IActionResult> UpdateCommentAsync
            (int commentId, [FromBody] CommentUpdateDto comment, CancellationToken ct = default)
        {
            var result = await _commentService.UpdateCommentAsync(commentId, comment.Content, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Deletes a comment by its ID.
        /// </summary>        
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteCommentAsync
            (int commentId, CancellationToken ct = default)
        {
            var result = await _commentService.DeleteCommentAsync(commentId, ct);

            return result.ToActionResult();
        }
    }
}
