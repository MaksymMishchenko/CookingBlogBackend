using Microsoft.AspNetCore.Mvc;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(ICommentService commentService,
            ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        /// <summary>
        /// Adds a new comment to a post with the specified post ID.
        /// Performs validation on the post ID, the comment object, and the model state.
        /// If the post is found and the comment is successfully added, returns a success response.
        /// In case of invalid data or failure to add the comment, returns an error response.
        /// Handles exceptions such as a post not being found or unexpected errors.
        /// </summary>
        /// <param name="postId">The ID of the post to which the comment is being added.</param>
        /// <param name="comment">The comment to be added to the post.</param>
        /// <returns>An IActionResult indicating the result of the operation. Returns a success response if the comment is added, or an error response if validation fails or the post is not found.</returns>
        /// <response code="200">Successfully added the comment to the post.</response>
        /// <response code="400">Invalid post ID, null comment, or validation failure.</response>
        /// <response code="404">Post not found.</response>
        /// <response code="500">Unexpected error during the comment addition process.</response>
        [HttpPost("posts/{postId}")]
        public async Task<IActionResult> AddCommentAsync(int postId, [FromBody] Comment comment)
        {
            if (postId <= 0)
            {
                _logger.LogWarning("Post ID must be greater than zero. Received value: {PostId}", postId);

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Post ID must be greater than zero."));
            }

            if (comment == null)
            {
                _logger.LogWarning("Received null comment for post ID {PostId}.", postId);

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Comment cannot be null."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning
                    ("Validation failed for post ID {PostId}. Errors: {Errors}.", postId, string.Join(", ", errors));

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Validation failed.", errors));
            }
            try
            {
                if (await _commentService.AddCommentAsync(postId, comment))
                {
                    _logger.LogInformation("Successfully added comment for post ID {PostId}. Comment ID: {CommentId}", postId, comment.CommentId);

                    return Ok(CommentResponse.CreateSuccessResponse
                        ("Comment added successfully."));
                }
                else
                {
                    _logger.LogWarning("Failed to add comment for post ID {PostId}.", postId);

                    return BadRequest(CommentResponse.CreateErrorResponse
                        ($"Failed to add comment for post ID {postId}."));
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Post with ID {PostId} not found.", postId);

                return NotFound(CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding comment to post ID: {PostId}", postId);

                return StatusCode(500, CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
        }

        [HttpPut("{commentId}")]
        public async Task<IActionResult> UpdateCommentAsync(int commentId, [FromBody] EditCommentModel comment)
        {
            await _commentService.UpdateCommentAsync(commentId, comment);
            return Ok();
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteCommentAsync(int commentId)
        {
            if (commentId <= 0)
            {
                return BadRequest("Post ID must be greater than zero.");
            }
            await _commentService.DeleteCommentAsync(commentId);
            return Ok("Comment removed successfully");
        }
    }
}
