using Microsoft.AspNetCore.Authorization;
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

        public PostsController(IPostService postsService)
        {
            _postsService = postsService;
        }

        [HttpGet("GetAllPosts")]
        public async Task<IActionResult> GetAllPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int commentPageNumber = 1,
        [FromQuery] int commentsPerPage = 10,
        [FromQuery] bool includeComments = true
        )
        {
            var posts = await _postsService.GetAllPostsAsync(pageNumber,
                pageSize,
                commentPageNumber,
                commentsPerPage,
                includeComments);
            return Ok(posts);
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(int postId, [FromQuery] bool includeComments)
        {
            var post = await _postsService.GetPostByIdAsync(postId, includeComments);
            return Ok(post);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddPost([FromBody] Post post)
        {
            await _postsService.AddPostAsync(post);
            return Ok();
        }

        [Authorize]
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

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            await _postsService.DeletePostAsync(id);
            return Ok();
        }
    }
}
