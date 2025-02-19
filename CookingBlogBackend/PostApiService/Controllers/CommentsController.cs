using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
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
        /// Adds a comment to a specific post.
        /// </summary>
        /// <param name="postId">The ID of the post to which the comment should be added.</param>
        /// <param name="comment">The comment to be added.</param>
        /// <returns>
        /// Returns a 200 OK response if the comment is successfully added.
        /// Returns a 400 Bad Request response if the post ID is invalid, the comment is null, 
        /// or if the model validation fails.
        /// </returns>     
        [HttpPost("{postId}")]
        public async Task<IActionResult> AddCommentAsync(int postId, [FromBody] Comment comment)
        {
            if (postId <= 0)
            {
                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (PostErrorMessages.InvalidPostIdParameter));
            }

            if (comment == null)
            {
                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (CommentErrorMessages.CommentCannotBeNull));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (CommentErrorMessages.ValidationFailed, errors));
            }

            if (comment.PostId != 0 && comment.PostId != postId)
            {
                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (CommentErrorMessages.MismatchedPostId));
            }

            await _commentService.AddCommentAsync(postId, comment);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentSuccessMessages.CommentAddedSuccessfully));
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

            await _commentService.UpdateCommentAsync(commentId, comment);

            return Ok(CommentResponse.CreateSuccessResponse
                ("Comment updated successfully."));
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
