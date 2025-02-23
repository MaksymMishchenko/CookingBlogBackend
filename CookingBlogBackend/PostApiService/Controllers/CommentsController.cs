using Microsoft.AspNetCore.Mvc;
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
        /// Updates an existing comment based on the provided comment ID.
        /// </summary>
        /// <param name="commentId">The ID of the comment to update.</param>
        /// <param name="comment">The updated comment data.</param>
        /// <returns>
        /// Returns a BadRequest response if the comment ID is invalid, the comment is null, 
        /// or the content is empty. If validation fails, returns a BadRequest response with error details. 
        /// Otherwise, returns an OK response indicating successful update.
        /// </returns>
        [HttpPut("{commentId}")]
        public async Task<IActionResult> UpdateCommentAsync(int commentId, [FromBody] EditCommentModel comment)
        {
            if (commentId <= 0)
            {
                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (CommentErrorMessages.InvalidCommentIdParameter));
            }

            if (comment == null)
            {
                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (CommentErrorMessages.CommentCannotBeNull));
            }

            if (string.IsNullOrEmpty(comment.Content))
            {
                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (CommentErrorMessages.ContentIsRequired));
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

            await _commentService.UpdateCommentAsync(commentId, comment);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentSuccessMessages.CommentUpdatedSuccessfully));
        }

        /// <summary>
        /// Deletes a comment by its ID.
        /// </summary>
        /// <param name="commentId">The ID of the comment to delete.</param>
        /// <returns>
        /// Returns a 200 OK response if the comment was successfully deleted.  
        /// Returns a 400 Bad Request response if the provided comment ID is invalid.
        /// </returns>
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteCommentAsync(int commentId)
        {
            if (commentId <= 0)
            {
                return BadRequest(ApiResponse<Comment>.CreateErrorResponse
                    (CommentErrorMessages.InvalidCommentIdParameter));
            }

            await _commentService.DeleteCommentAsync(commentId);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentSuccessMessages.CommentDeletedSuccessfully));
        }
    }
}
