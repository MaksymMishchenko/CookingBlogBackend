using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Enums;
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
        [ValidateModel]        
        [ValidateId(InvalidIdErrorMessage = PostErrorMessages.InvalidPostIdParameter, ErrorResponseType = ResourceType.Post)]
        public async Task<IActionResult> AddCommentAsync(int postId, [FromBody] Comment comment)
        {
            await _commentService.AddCommentAsync(postId, comment);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentSuccessMessages.CommentAddedSuccessfully));
        }

        /// <summary>
        /// Updates an existing comment based on the provided comment ID.
        /// </summary>        
        [HttpPut("{commentId}")]
        [ValidateId(InvalidIdErrorMessage = CommentErrorMessages.InvalidCommentIdParameter, ErrorResponseType = ResourceType.Comment)]
        public async Task<IActionResult> UpdateCommentAsync(int commentId, [FromBody] EditCommentModel comment)
        {
            await _commentService.UpdateCommentAsync(commentId, comment);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentSuccessMessages.CommentUpdatedSuccessfully));
        }

        /// <summary>
        /// Deletes a comment by its ID.
        /// </summary>        
        [HttpDelete("{commentId}")]
        [ValidateId(InvalidIdErrorMessage = CommentErrorMessages.InvalidCommentIdParameter, ErrorResponseType = ResourceType.Comment)]
        public async Task<IActionResult> DeleteCommentAsync(int commentId)
        {
            await _commentService.DeleteCommentAsync(commentId);

            return Ok(ApiResponse<Comment>.CreateSuccessResponse
                (CommentSuccessMessages.CommentDeletedSuccessfully));
        }
    }
}
