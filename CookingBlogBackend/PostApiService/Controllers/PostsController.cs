using Microsoft.AspNetCore.Mvc;
using PostApiService.Exceptions;
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
        /// <param name="cancellationToken">A token that can be used to cancel the request. Defaults to <c>default</c>.</param>
        /// <returns>A list of posts with optional comments, or an error response.</returns>        
        [HttpGet("GetAllPosts")]
        public async Task<IActionResult> GetAllPostsAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int commentPageNumber = 1,
        [FromQuery] int commentsPerPage = 10,
        [FromQuery] bool includeComments = true,
        CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1 || pageSize < 1 || commentPageNumber < 1 || commentsPerPage < 1)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    (ErrorMessages.InvalidPageParameters));
            }

            if (pageSize > 10 || commentsPerPage > 10)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    (ErrorMessages.PageSizeExceeded));
            }

            var posts = await _postsService.GetAllPostsAsync(
                pageNumber,
                pageSize,
                commentPageNumber,
                commentsPerPage,
                includeComments,
                cancellationToken);

            if (!posts.Any())
            {
                return NotFound(PostResponse.CreateErrorResponse
                    (ErrorMessages.NoPostsFound));
            }

            return Ok(PostResponse.CreateSuccessResponse(string.Format
                (SuccessMessages.PostsRetrievedSuccessfully, posts.Count),
                posts));
        }

        /// <summary>
        /// Retrieves a specific post by its ID. Optionally includes comments if specified.
        /// </summary>
        /// <param name="postId">The ID of the post to retrieve.</param>
        /// <param name="includeComments">A flag indicating whether to include comments in the response. Defaults to true.</param>
        /// <returns>Returns an IActionResult with the post data if found, or a BadRequest if the postId is invalid.</returns>
        [HttpGet("GetPost/{postId}", Name = "GetPostById")]
        public async Task<IActionResult> GetPostByIdAsync(int postId, [FromQuery] bool includeComments = true)
        {
            if (postId < 1)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    (ErrorMessages.InvalidPageParameters));
            }

            var post = await _postsService.GetPostByIdAsync(postId, includeComments);

            return Ok(post);
        }

        /// <summary>
        /// Adds a new post to the system.
        /// </summary>
        /// <param name="post">The post object to be added. It contains details such as title, content, etc.</param>
        /// <returns>
        /// Returns a <see cref="CreatedAtActionResult"/> if the post is successfully added,
        /// with a 201 status code and a success response message. If the post is null, it returns a 
        /// BadRequest with an error message. If validation fails, it returns a BadRequest with validation errors.        
        /// </returns>
        /// <response code="201">Post successfully created.</response>
        /// <response code="400">Bad request if the post is null or validation fails.</response>        
        [HttpPost("AddNewPost")]
        public async Task<IActionResult> AddPostAsync([FromBody] Post post)
        {
            if (post == null)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    (ErrorMessages.PostCannotBeNull));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(PostResponse.CreateErrorResponse
                    (ErrorMessages.ValidationFailed, errors));
            }

            var addedPost = await _postsService.AddPostAsync(post);

            return CreatedAtAction("GetPostById", new { postId = addedPost.PostId },
                PostResponse.CreateSuccessResponse
                (SuccessMessages.PostAddedSuccessfully, addedPost.PostId));
        }

        /// <summary>
        /// Updates an existing post.
        /// </summary>
        /// <param name="post">The post object containing updated data.</param>
        /// <returns>
        /// Returns an HTTP response:
        /// - 200 OK if the post was successfully updated.
        /// - 400 Bad Request if the post is null, has an invalid ID, or fails validation.        
        [HttpPut]
        public async Task<IActionResult> UpdatePostAsync([FromBody] Post post)
        {
            if (post == null || post.PostId <= 0)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    (ErrorMessages.InvalidPostOrId));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(PostResponse.CreateErrorResponse
                    (ErrorMessages.ValidationFailed, errors));
            }

            await _postsService.UpdatePostAsync(post);

            return Ok(PostResponse.CreateSuccessResponse
                (string.Format
                (SuccessMessages.PostUpdatedSuccessfully, post.PostId), post.PostId));
        }

        /// <summary>
        /// Deletes a post by its ID.
        /// </summary>
        /// <param name="postId">The ID of the post to be deleted.</param>
        /// <returns>
        /// - 200 OK if the post was successfully deleted.  
        /// - 400 Bad Request if the provided ID is invalid.  
        /// - 409 Conflict if deletion failed due to an invalid operation.  
        /// - 500 Internal Server Error if an unexpected error or database issue occurs.
        /// </returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePostAsync(int postId)
        {
            if (postId <= 0)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    ("Parameters must be greater than 0."));
            }

            try
            {
                await _postsService.DeletePostAsync(postId);
                return Ok(PostResponse.CreateSuccessResponse
                    ("Post was deleted successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Post with the specified ID not found.");
                return NotFound(PostResponse.CreateErrorResponse
                    ($"Post with ID {postId} not found. Please check the Post ID."));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation occurred while deleting post with ID {PostId}.", postId);
                return Conflict(PostResponse.CreateErrorResponse
                    ($"Failed to delete post with ID {postId}."));
            }
            catch (Exception ex)
            {
                var requestId = Guid.NewGuid();
                _logger.LogError(ex, "Unexpected error occurred while deleting the post. Request ID: {RequestId}", requestId);
                return StatusCode(500, PostResponse.CreateErrorResponse
                    ($"An unexpected error occurred. Request ID: {requestId}. Please contact support."));
            }
        }
    }
}
