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
        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            var posts = await _postsService.GetAllPostsAsync();
            return Ok(posts);
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(int postId)
        {
            var post = await _postsService.GetPostByIdAsync(postId);            
            return Ok(post);
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
    }
}
