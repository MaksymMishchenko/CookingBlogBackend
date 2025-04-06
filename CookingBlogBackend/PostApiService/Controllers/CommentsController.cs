using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    [Authorize(Policy = TS.Policies.ContributorPolicy)]
    public class CommentsController : Controller
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

            await _commentService.UpdateCommentAsync(commentId, comment);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentSuccessMessages.CommentUpdatedSuccessfully));
        }

        /// <summary>
        /// Deletes a comment by its ID.
        /// </summary>        
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
