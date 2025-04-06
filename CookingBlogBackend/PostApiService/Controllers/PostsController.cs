using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = TS.Policies.FullControlPolicy)]
    public class PostsController : Controller
    {
        private readonly IPostService _postsService;       

        public PostsController(IPostService postsService)
        {
            _postsService = postsService;            
        }

        /// <summary>
        /// Retrieves a paginated list of posts from the database with optional comments.        
        /// </summary>            
        [HttpGet]
        [AllowAnonymous]
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
                return BadRequest(ApiResponse<Post>.CreateErrorResponse
                    (PostErrorMessages.InvalidPageParameters));
            }

            if (pageSize > 10 || commentsPerPage > 10)
            {
                return BadRequest(ApiResponse<Post>.CreateErrorResponse
                    (PostErrorMessages.PageSizeExceeded));
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
                return NotFound(ApiResponse<Post>.CreateErrorResponse
                    (PostErrorMessages.NoPostsFound));
            }

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format(PostSuccessMessages.PostsRetrievedSuccessfully, posts.Count), posts));
        }

        /// <summary>
        /// Retrieves a specific post by its ID. Optionally includes comments if specified.
        /// </summary>        
        [HttpGet("{postId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostByIdAsync(int postId, [FromQuery] bool includeComments = true)
        {
            if (postId < 1)
            {
                return BadRequest(ApiResponse<Post>.CreateErrorResponse
                    (PostErrorMessages.InvalidPageParameters));
            }

            var post = await _postsService.GetPostByIdAsync(postId, includeComments);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format
                (PostSuccessMessages.PostRetrievedSuccessfully, post.Id), post));
        }

        /// <summary>
        /// Adds a new post to the system.
        /// </summary>               
        [HttpPost]
        public async Task<IActionResult> AddPostAsync([FromBody] Post post)
        {
            var addedPost = await _postsService.AddPostAsync(post);

            return CreatedAtAction("GetPostById", new { postId = addedPost.Id },
                ApiResponse<Post>.CreateSuccessResponse
                (string.Format(PostSuccessMessages.PostAddedSuccessfully), addedPost.Id));
        }

        /// <summary>
        /// Updates an existing post.
        /// </summary>               
        [HttpPut]
        public async Task<IActionResult> UpdatePostAsync([FromBody] Post post)
        {
            if (post == null || post.Id <= 0)
            {
                return BadRequest(ApiResponse<Post>.CreateErrorResponse
                    (PostErrorMessages.InvalidPostOrId));
            }

            await _postsService.UpdatePostAsync(post);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format
                (PostSuccessMessages.PostUpdatedSuccessfully, post.Id), post.Id));
        }

        /// <summary>
        /// Deletes a post by its ID.
        /// </summary>        
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePostAsync(int postId)
        {
            if (postId <= 0)
            {
                return BadRequest(ApiResponse<Post>.CreateErrorResponse
                    (PostErrorMessages.InvalidPostIdParameter));
            }

            await _postsService.DeletePostAsync(postId);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format
                (PostSuccessMessages.PostDeletedSuccessfully, postId), postId));
        }
    }
}
