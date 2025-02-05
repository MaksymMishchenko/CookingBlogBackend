using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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
                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Post ID must be greater than zero."));
            }

            if (comment == null)
            {
                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Comment cannot be null."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Validation failed.", errors));
            }
            try
            {
                await _commentService.AddCommentAsync(postId, comment);

                return Ok(CommentResponse.CreateSuccessResponse
                    ("Comment added successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Post with ID {PostId} not found.", postId);

                return NotFound(CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation occurred while adding comment to post with ID {PostId}.", postId);
                return Conflict(CommentResponse.CreateErrorResponse
                    ($"Failed to add comment to post with ID {postId}."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding comment to post ID: {PostId}", postId);
                return StatusCode(500, CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
        }

        /// <summary>
        /// Updates an existing comment with the specified comment ID and new content.
        /// Performs validation on the comment ID, content, and model state.
        /// If the comment is found and successfully updated, returns a success response with the updated content.
        /// In case of validation failure, a missing comment, or concurrency issues, returns an appropriate error response.
        /// Handles exceptions for not found comments, concurrency issues, and other unexpected errors.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be updated.</param>
        /// <param name="comment">The updated comment data, including the new content.</param>
        /// <returns>An IActionResult indicating the result of the operation. Returns a success response if the comment is updated, or an error response if validation fails, the comment is not found, or there is a concurrency issue.</returns>
        /// <response code="200">Successfully updated the comment.</response>
        /// <response code="400">Invalid comment ID, null comment, missing content, or validation failure.</response>
        /// <response code="404">Comment not found.</response>
        /// <response code="409">Concurrency issue occurred while updating the comment.</response>
        /// <response code="500">Unexpected error during the comment update process.</response>
        [HttpPut("{commentId}")]
        public async Task<IActionResult> UpdateCommentAsync(int commentId, [FromBody] EditCommentModel comment)
        {
            if (commentId <= 0)
            {
                _logger.LogWarning("Comment ID must be greater than zero. Received value: {PostId}", commentId);

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Comment ID must be greater than zero."));
            }

            if (comment == null)
            {
                _logger.LogWarning("Received null comment for comment ID {PostId}.", commentId);

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Comment cannot be null."));
            }

            if (string.IsNullOrEmpty(comment.Content))
            {
                _logger.LogWarning("Content is required for editing comment ID {CommentId}.", commentId);

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Content is required."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Validation failed for comment ID {CommentId}. Errors: {Errors}.", commentId, string.Join(", ", errors));

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Validation failed.", errors));
            }

            try
            {
                var success = await _commentService.UpdateCommentAsync(commentId, comment);

                if (success)
                {
                    return Ok(CommentResponse.CreateSuccessResponse
                        ("Comment updated successfully."));
                }
                else
                {
                    _logger.LogWarning("Failed to update comment ID {commentId}.", commentId);

                    return BadRequest(CommentResponse.CreateErrorResponse
                        ($"Failed to update comment ID {commentId}."));
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Comment with ID {CommentId} not found.", commentId);

                return NotFound(CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency issue occurred while updating comment ID {commentId}.", commentId);

                return StatusCode(409, CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating comment ID: {PostId}", commentId);

                return StatusCode(500, CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
        }

        /// <summary>
        /// Deletes a comment with the specified comment ID.
        /// Validates the comment ID and performs deletion using the comment service.
        /// If the comment is found and successfully deleted, returns a success response.
        /// In case of validation failure, missing comment, or concurrency issues, returns an appropriate error response.
        /// Handles exceptions for not found comments, concurrency issues, and other unexpected errors.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be deleted.</param>
        /// <returns>An IActionResult indicating the result of the operation. Returns a success response if the comment is deleted, or an error response if validation fails, the comment is not found, or there is a concurrency issue.</returns>
        /// <response code="200">Successfully deleted the comment.</response>
        /// <response code="400">Invalid comment ID.</response>
        /// <response code="404">Comment not found.</response>
        /// <response code="409">Concurrency issue occurred while deleting the comment.</response>
        /// <response code="500">Unexpected error during the comment deletion process.</response>
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteCommentAsync(int commentId)
        {
            if (commentId <= 0)
            {
                _logger.LogWarning("Comment ID must be greater than zero. Received value: {CommentId}", commentId);

                return BadRequest(CommentResponse.CreateErrorResponse
                    ("Comment ID must be greater than zero."));
            }

            try
            {
                await _commentService.DeleteCommentAsync(commentId);

                return Ok(CommentResponse.CreateSuccessResponse("Comment deleted successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Comment with ID {CommentId} not found.", commentId);

                return NotFound(CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency issue occurred while deleting comment ID {CommentId}.", commentId);

                return StatusCode(409, CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while removing comment ID: {CommentId}", commentId);

                return StatusCode(500, CommentResponse.CreateErrorResponse
                    (ex.Message));
            }
        }
    }
}
