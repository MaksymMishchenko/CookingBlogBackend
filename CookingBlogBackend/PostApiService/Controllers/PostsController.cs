using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Dto;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Enums;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = TS.Policies.FullControlPolicy)]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postsService;

        public PostsController(IPostService postsService)
        {
            _postsService = postsService;
        }

        /// <summary>
        /// Retrieves a paginated list of posts and the total count of all posts in the database.        
        /// </summary>           
        [HttpGet]
        [AllowAnonymous]
        [ValidatePaginationParameters]
        public async Task<IActionResult> GetPostsWithTotalPostCountAsync([FromQuery] PostQueryParameters query,
        CancellationToken cancellationToken = default)
        {
            var (posts, totalPostCount) = await _postsService.GetPostsWithTotalPostCountAsync(
                query.PageNumber,
                query.PageSize,
                cancellationToken);

            if (!posts.Any())
            {
                return NotFound(ApiResponse<PostListDto>.CreateErrorResponse
                    (PostErrorMessages.NoPostsFound));
            }

            return Ok(ApiResponse<PostListDto>.CreatePaginatedListResponse
                (string.Format(PostSuccessMessages.PostsRetrievedSuccessfully, posts.Count),
                posts,
                query.PageNumber,
                query.PageSize,
                totalPostCount));
        }

        /// <summary>
        /// Retrieves a specific post by its ID. Optionally includes comments if specified.
        /// </summary>        
        [HttpGet("{postId}")]
        [AllowAnonymous]
        [ValidateId(InvalidIdErrorMessage = PostErrorMessages.InvalidPostIdParameter, ErrorResponseType = ResourceType.Post)]
        public async Task<IActionResult> GetPostByIdAsync(int postId, [FromQuery] bool includeComments = true)
        {
            var post = await _postsService.GetPostByIdAsync(postId, includeComments);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format
                (PostSuccessMessages.PostRetrievedSuccessfully, post.Id), post));
        }

        /// <summary>
        /// Adds a new post to the system.
        /// </summary>               
        [HttpPost]
        [ValidateModel(InvalidErrorMessage = ResponseErrorMessages.ValidationFailed, ErrorResponseType = ResourceType.Post)]
        public async Task<IActionResult> AddPostAsync([FromBody] Post post)
        {
            var addedPost = await _postsService.AddPostAsync(post);

            return CreatedAtAction("GetPostById", new { postId = addedPost.Id },
                ApiResponse<Post>.CreateSuccessResponse
                (string.Format(PostSuccessMessages.PostAddedSuccessfully), addedPost));
        }

        /// <summary>
        /// Updates an existing post.
        /// </summary>               
        [HttpPut("{postId}")]
        [ValidateModel(InvalidErrorMessage = ResponseErrorMessages.ValidationFailed, ErrorResponseType = ResourceType.Post)]
        [ValidateId(InvalidIdErrorMessage = PostErrorMessages.InvalidPostIdParameter, ErrorResponseType = ResourceType.Post)]
        public async Task<IActionResult> UpdatePostAsync(int postId, [FromBody] Post post)
        {
            var updatedPost = await _postsService.UpdatePostAsync(postId, post);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format
                (PostSuccessMessages.PostUpdatedSuccessfully, postId), updatedPost));
        }

        /// <summary>
        /// Deletes a post by its ID.
        /// </summary>        
        [HttpDelete("{postId}")]
        [ValidateId(InvalidIdErrorMessage = PostErrorMessages.InvalidPostIdParameter, ErrorResponseType = ResourceType.Post)]
        public async Task<IActionResult> DeletePostAsync(int postId)
        {
            await _postsService.DeletePostAsync(postId);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format
                (PostSuccessMessages.PostDeletedSuccessfully, postId), postId));
        }
    }
}
