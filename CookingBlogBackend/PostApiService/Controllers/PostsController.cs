using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// Retrieves a paginated list of posts, optionally including paginated comments for each post.
        /// </summary>
        /// <param name="pageNumber">The page number for the list of posts (default is 1).</param>
        /// <param name="pageSize">The number of posts per page (default is 10).</param>
        /// <param name="commentPageNumber">The page number for comments within each post (default is 1).</param>
        /// <param name="commentsPerPage">The number of comments per page for each post (default is 10).</param>
        /// <param name="includeComments">Specifies whether to include comments with the posts (default is true).</param>
        /// <returns>
        /// An HTTP 200 response with a paginated list of posts and optionally their comments if successful.
        /// An HTTP 400 response if any of the parameters are invalid (less than 1).
        /// An HTTP 500 response if an unexpected error occurs.
        /// </returns>
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

        [HttpGet("GetPost/{postId}", Name = "GetPostById")]
        public async Task<IActionResult> GetPostByIdAsync(int postId, [FromQuery] bool includeComments = true)
        {
            var post = await _postsService.GetPostByIdAsync(postId, includeComments);
            return Ok(post);
        }

        /// <summary>
        /// Adds a new post to the system. This action is restricted to authorized users only.
        /// It validates the post data, attempts to add it to the database, and returns a response
        /// indicating the result of the operation. If successful, it returns a 201 Created response
        /// with the URL of the newly created post. If validation fails or an error occurs during
        /// the addition process, it returns an appropriate error response.
        /// </summary>
        /// <param name="post">The post data to be added to the system. Must be a valid Post object.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an IActionResult,
        /// which can be a success (201 Created) or an error response (400, 500, or 409 depending on the error).
        /// </returns> 
        [Authorize]
        [HttpPost("AddNewPost")]        
        public async Task<IActionResult> AddPostAsync([FromBody] Post post)
        {
            if (post == null)
            {
                return BadRequest(new { Message = "Post cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    Success = false,
                    Message = "Validation failed.",
                    Errors = errors
                });
            }

            try
            {
                var result = await _postsService.AddPostAsync(post);

                if (result.Success)
                {
                    return CreatedAtAction("GetPostById", new { postId = result.PostId }, new
                    {
                        Success = true,
                        postId = result.PostId,
                        Message = "Post added successfully."
                    });
                }

                _logger.LogWarning("Failed to add post with title: {Title}.", post.Title);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "Failed to add the post."
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while adding post with title: {Title}.", post.Title);
                return Conflict(new
                {
                    Success = false,
                    Message = "A database error occurred. Please try again later."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding post with title: {Title}.", post.Title);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while processing your request."
                });
            }
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
