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
        /// Retrieves a paginated list of posts from the database with optional comments.
        /// Supports pagination for both posts and comments. Returns an error response 
        /// if parameters are invalid or if no posts are found for the requested page.
        /// </summary>
        /// <param name="pageNumber">The page number for posts. Defaults to 1.</param>
        /// <param name="pageSize">The number of posts per page. Defaults to 10.</param>
        /// <param name="commentPageNumber">The page number for comments. Defaults to 1.</param>
        /// <param name="commentsPerPage">The number of comments per page. Defaults to 10.</param>
        /// <param name="includeComments">Indicates whether to include comments in the response. Defaults to true.</param>
        /// <returns>A list of posts with optional comments, or an error response.</returns>
        /// <response code="200">Returns a list of posts with comments if the request is successful.</response>
        /// <response code="400">If any of the parameters are invalid (less than 1 or exceed the allowed maximum).</response>
        /// <response code="404">If no posts are found for the requested page.</response>
        /// <response code="500">If an unexpected error occurs while processing the request.</response>
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
                return BadRequest(PostResponse.CreateErrorResponse
                    ("Parameters must be greater than 0."));
            }

            if (pageSize > 10 || commentsPerPage > 10)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    ("Page size or comments per page exceeds the allowed maximum."));
            }

            try
            {
                var posts = await _postsService.GetAllPostsAsync(
                    pageNumber,
                    pageSize,
                    commentPageNumber,
                    commentsPerPage,
                    includeComments);

                if (!posts.Any())
                {
                    return NotFound(PostResponse.CreateErrorResponse
                        ("No posts found for the requested page."));
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching posts.");
                return StatusCode(StatusCodes.Status500InternalServerError, PostResponse.CreateErrorResponse
                    ("An error occurred while processing your request."));
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

                if (result)
                {
                    return CreatedAtAction("GetPostById", new
                    {
                        Success = true,
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePostAsync(int id, [FromBody] Post post)
        {
            if (id != post.PostId)
            {
                return BadRequest();
            }

            await _postsService.UpdatePostAsync(post);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePostAsync(int id)
        {
            await _postsService.DeletePostAsync(id);
            return Ok();
        }
    }
}
