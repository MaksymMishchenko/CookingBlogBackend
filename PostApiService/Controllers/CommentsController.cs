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
        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpPost("posts/{postId}")]
        public async Task<IActionResult> AddCommentAsync(int postId, [FromBody] Comment comment)
        {
            if (postId <= 0)
            {
                return BadRequest("Post ID must be greater than zero.");
            }

            if (comment == null)
            {
                return BadRequest("Comment cannot be null.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new { Errors = errors });
            }

            var result = await _commentService.AddCommentAsync(postId, comment);
            return Ok(result);
        }

        [HttpPut("{commentId}")]
        public async Task<IActionResult> EditCommentAsync(int commentId, [FromBody] Comment comment)
        {
            if (commentId != comment.CommentId)
            {
                return BadRequest();
            }

            await _commentService.EditCommentAsync(comment);
            return Ok();
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteCommentAsync(int commentId)
        {
            if (commentId <= 0)
            {
                return BadRequest("Post ID must be greater than zero.");
            }
            var result = await _commentService.DeleteCommentAsync(commentId);
            return Ok(result);
        }
    }
}
