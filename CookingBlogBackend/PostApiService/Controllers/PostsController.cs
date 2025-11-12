using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
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
        /// Allows optional inclusion and pagination of comments.
        /// </summary>           
        [HttpGet]
        [AllowAnonymous]
        [ValidatePostQueryParameters]
        public async Task<IActionResult> GetPostsWithTotalAsync([FromQuery] PostQueryParameters query,
        CancellationToken cancellationToken = default)
        {
            var (posts, totalCount) = await _postsService.GetPostsWithTotalAsync(
                query.PageNumber,
                query.PageSize,
                query.CommentPageNumber,
                query.CommentsPerPage,
                query.IncludeComments,
                cancellationToken);

            if (!posts.Any())
            {
                return NotFound(ApiResponse<Post>.CreateErrorResponse
                    (PostErrorMessages.NoPostsFound));
            }

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format(PostSuccessMessages.PostsRetrievedSuccessfully, posts.Count),
                posts,
                query.PageNumber,
                query.PageSize,
                totalCount));
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
                (string.Format(PostSuccessMessages.PostAddedSuccessfully), addedPost.Id));
        }

        /// <summary>
        /// Updates an existing post.
        /// </summary>               
        [HttpPut("{postId}")]
        [ValidateModel(InvalidErrorMessage = ResponseErrorMessages.ValidationFailed, ErrorResponseType = ResourceType.Post)]
        [ValidateId(InvalidIdErrorMessage = PostErrorMessages.InvalidPostIdParameter, ErrorResponseType = ResourceType.Post)]
        public async Task<IActionResult> UpdatePostAsync(int postId, [FromBody] Post post)
        {
            await _postsService.UpdatePostAsync(postId, post);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format
                (PostSuccessMessages.PostUpdatedSuccessfully, postId), postId));
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
