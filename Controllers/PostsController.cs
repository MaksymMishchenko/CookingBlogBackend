using Microsoft.AspNetCore.Mvc;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : Controller
    {
        private readonly IPostService _postsService;
        private readonly ICommentService _commentService;

        public PostsController(IPostService postsService, ICommentService commentService)
        {
            _postsService = postsService;
            _commentService = commentService;
        }
        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            var posts = await _postsService.GetAllPosts();
            return Ok(posts);
        }

        [HttpPost]
        public async Task<IActionResult> AddPost([FromBody] Post post)
        {
            await _postsService.AddPostAsync(post);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditPost(int id, [FromBody] Post post)
        {
            if (id != post.PostId)
            {
                return BadRequest();
            }

            await _postsService.EditPostAsync(post);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            await _postsService.DeletePostAsync(id);
            return Ok();
        }

        [HttpPost("{postId}/comments")]
        public async Task<IActionResult> AddComment(int postId, [FromBody] Comment comment)
        {
            await _commentService.AddCommentAsync(postId, comment);
            return Ok();
        }

        [HttpPut("comments/{commentId}")]
        public async Task<IActionResult> EditComment(int commentId, [FromBody] Comment comment)
        {
            if (commentId != comment.CommentId)
            {
                return BadRequest();
            }

            await _commentService.EditCommentAsync(comment);
            return Ok();
        }

        [HttpDelete("comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            await _commentService.DeleteCommentAsync(commentId);
            return Ok();
        }
    }
}
