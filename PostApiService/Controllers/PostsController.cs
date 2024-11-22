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
        private readonly ILogger<PostsController> _logger;

        public PostsController(IPostService postsService, ILogger<PostsController> logger)
        {
            _postsService = postsService;
            _logger = logger;
        }

        [HttpGet("GetAllPosts")]
        public async Task<IActionResult> GetAllPostsAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int commentPageNumber = 1,
        [FromQuery] int commentsPerPage = 10,
        [FromQuery] bool includeComments = true
        )
        {            
            if (pageNumber < 1 || pageSize < 1 || commentPageNumber < 1 || commentsPerPage < 1)
            {
                return BadRequest("Parameters must be greater than 0.");
            }

            try
            {
                var posts = await _postsService.GetAllPostsAsync(
                    pageNumber,
                    pageSize,
                    commentPageNumber,
                    commentsPerPage,
                    includeComments);

                return Ok(posts);
            }

            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters provided.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching posts.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostByIdAsync(int postId, [FromQuery] bool includeComments)
        {
            var post = await _postsService.GetPostByIdAsync(postId, includeComments);
            return Ok(post);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddPostAsync([FromBody] Post post)
        {
            await _postsService.AddPostAsync(post);
            return Ok();
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> EditPostAsync(int id, [FromBody] Post post)
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
        public async Task<IActionResult> DeletePostAsync(int id)
        {
            await _postsService.DeletePostAsync(id);
            return Ok();
        }
    }
}
