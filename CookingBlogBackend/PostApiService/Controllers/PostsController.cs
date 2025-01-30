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

        /// <summary>
        /// Retrieves a post by its ID from the database, with an option to include comments.
        /// </summary>
        /// <param name="postId">The unique identifier of the post to retrieve. Must be greater than 0.</param>
        /// <param name="includeComments">
        /// A boolean flag indicating whether to include comments in the response. 
        /// Defaults to <c>true</c>.
        /// </param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing:
        /// <list type="bullet">
        /// <item><c>200 OK</c> with the post data if found.</item>
        /// <item><c>400 Bad Request</c> if the <paramref name="postId"/> is invalid (less than 1).</item>
        /// <item><c>404 Not Found</c> if no post with the specified <paramref name="postId"/> exists.</item>
        /// <item><c>500 Internal Server Error</c> if an unexpected error occurs.</item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Logs warnings for invalid input or if the post is not found. Logs an error for unexpected exceptions.
        /// </remarks>
        [HttpGet("GetPost/{postId}", Name = "GetPostById")]
        public async Task<IActionResult> GetPostByIdAsync(int postId, [FromQuery] bool includeComments = true)
        {
            if (postId < 1)
            {
                _logger.LogWarning("Invalid postId: {PostId}. Parameters must be greater than 0.", postId);
                return BadRequest(PostResponse.CreateErrorResponse("Parameters must be greater than 0."));
            }

            try
            {
                var post = await _postsService.GetPostByIdAsync(postId, includeComments);

                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Post with id {PostId} not found.", postId);
                return NotFound(PostResponse.CreateErrorResponse($"Post with id {postId} not found."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing request to get post by id {PostId}.", postId);
                return StatusCode(500, PostResponse.CreateErrorResponse($"An error occurred while processing request to get post by id {postId}."));
            }
        }

        /// <summary>
        /// Adds a new post to the database.
        /// </summary>
        /// <param name="post">The post object containing the details to be added.</param>
        /// <returns>
        /// - **201 Created**: If the post was successfully added, returns the created post's ID in the response.
        /// - **400 Bad Request**: If the provided post object is null or fails validation, includes detailed error information.
        /// - **409 Conflict**: If a post with the same title already exists in the database.
        /// - **500 Internal Server Error**: If an unexpected error occurs during the operation.
        /// </returns>
        /// <remarks>
        /// This method validates the provided `Post` object before attempting to add it to the database. 
        /// If the operation is successful, it returns a response with the location of the created post.
        /// </remarks>        
        [HttpPost("AddNewPost")]
        public async Task<IActionResult> AddPostAsync([FromBody] Post post)
        {
            if (post == null)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    ("Post cannot be null."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                _logger.LogWarning("Validation failed. Errors: {Errors}", string.Join(", ", errors.SelectMany(e => e.Value)));

                return BadRequest(PostResponse.CreateErrorResponse
                    ("Validation failed.", errors));
            }

            try
            {
                var addedPost = await _postsService.AddPostAsync(post);

                if (addedPost != null)
                {
                    return CreatedAtAction("GetPostById", new { postId = addedPost.PostId }, PostResponse.CreateSuccessResponse
                        ("Post added successfully.", addedPost.PostId));
                }

                return Conflict(PostResponse.CreateErrorResponse
                    ("A post with this title already exists."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding post with title: {Title}.", post.Title);
                return StatusCode(StatusCodes.Status500InternalServerError, PostResponse.CreateErrorResponse
                    ("An unexpected error occurred while adding post"));
            }
        }

        /// <summary>
        /// Updates an existing post. Validates the input and handles various exceptions.
        /// If the post is successfully updated, it returns a success response. 
        /// If the post cannot be updated due to validation issues, ID mismatch, or database errors, 
        /// appropriate error responses are returned.
        /// </summary>
        /// <param name="id">The ID of the post to be updated.</param>
        /// <param name="post">The updated post data.</param>
        /// <returns>An IActionResult representing the result of the operation.</returns>       
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePostAsync(int id, [FromBody] Post post)
        {
            if (post == null || post.PostId != id || id <= 0)
            {
                return BadRequest(PostResponse.CreateErrorResponse
                    ("Post cannot be null, ID mismatch, or ID should be greater than 0."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                _logger.LogWarning("Validation failed. Errors: {Errors}", string.Join(", ", errors.SelectMany(e => e.Value)));

                return BadRequest(PostResponse.CreateErrorResponse
                    ("Validation failed.", errors));
            }

            try
            {
                var isUpdated = await _postsService.UpdatePostAsync(post);

                if (isUpdated)
                {
                    return Ok(PostResponse.CreateSuccessResponse($"Post with Post Id {post.PostId} updated successfully.", id));
                }

                return Conflict(PostResponse.CreateErrorResponse($"No changes were made to post with ID {post.PostId}."));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Post with the specified ID not found.");
                return NotFound(PostResponse.CreateErrorResponse($"Post with ID {post.PostId} not found. Please check the Post ID."));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "No changes were made to post with ID {PostId}.");
                return Conflict(PostResponse.CreateErrorResponse($"No changes were made to post with ID {post.PostId}."));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Database concurrency error occurred while updating the post.");
                return Conflict(PostResponse.CreateErrorResponse("A concurrency error occurred while updating the post. Please try again later."));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed for post.");
                return StatusCode(500, "A database error occurred while updating the post. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating the post.");
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePostAsync(int id)
        {
            await _postsService.DeletePostAsync(id);
            return Ok();
        }
    }
}
