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
        public async Task<IActionResult> AddComment(int postId, [FromBody] Comment comment)
        {
            await _commentService.AddCommentAsync(postId, comment);
            return Ok();
        }

        [HttpPut("{commentId}")]
        public async Task<IActionResult> EditComment(int commentId, [FromBody] Comment comment)
        {
            if (commentId != comment.CommentId)
            {
                return BadRequest();
            }

            await _commentService.EditCommentAsync(comment);
            return Ok();
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            await _commentService.DeleteCommentAsync(commentId);
            return Ok();
        }
    }
}
