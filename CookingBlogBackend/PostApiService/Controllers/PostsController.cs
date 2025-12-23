using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
using PostApiService.Controllers.Filters.PostApiService.Controllers.Filters;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Dto;
using PostApiService.Models.Dto.Requests;
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
        /// Retrieves a paginated collection of posts and the total record count. 
        /// Returns a success message with the count or an information message if the database is empty.
        /// </summary>          
        [HttpGet]
        [AllowAnonymous]
        [ValidatePaginationParameters]
        public async Task<IActionResult> GetPostsWithTotalPostCountAsync([FromQuery] PostQueryParameters query,
        CancellationToken cancellationToken = default)
        {
            var (posts, totalPostCount) = await _postsService.GetPostsWithTotalPostCountAsync
                (query.PageNumber, query.PageSize, cancellationToken);

            string message = posts.Any()
                ? string.Format(PostSuccessMessages.PostsRetrievedSuccessfully, posts.Count)
                : string.Format(PostSuccessMessages.NoPostsAvailableYet);

            return Ok(ApiResponse<PostListDto>.CreatePaginatedListResponse
                (message, posts, query.PageNumber, query.PageSize, totalPostCount));
        }

        /// <summary>
        /// Searches posts by query string and returns a paginated list of results 
        /// along with the total count of matched records.                  
        [HttpGet("search")]
        [AllowAnonymous]
        [ValidateSearchQuery]
        [ValidatePaginationParameters]
        public async Task<IActionResult> SearchPostsWithTotalCountAsync([FromQuery] SearchPostQueryParameters query,
        CancellationToken cancellationToken = default)
        {
            var (searchPostList, searchTotalPosts) = await _postsService.SearchPostsWithTotalCountAsync
                (query.QueryString, query.PageNumber, query.PageSize, cancellationToken);

            string message = searchPostList.Any()
                ? string.Format(PostSuccessMessages.PostsRetrievedSuccessfully, query.PageSize)
                : string.Format(PostSuccessMessages.NoPostsFound, query.QueryString);

            return Ok(ApiResponse<SearchPostListDto>.CreatePaginatedSearchListResponse
                (message, query.QueryString, searchPostList, query.PageNumber, query.PageSize, searchTotalPosts));
        }

        /// <summary>
        /// Retrieves a post by its unique identifier.
        /// </summary>        
        [HttpGet("{postId}")]
        [ValidateId]
        public async Task<IActionResult> GetPostByIdAsync(int postId)
        {
            var post = await _postsService.GetPostByIdAsync(postId);

            return Ok(ApiResponse<PostAdminDetailsDto>.CreateSingleItemResponse
                (string.Format(PostSuccessMessages.PostRetrievedSuccessfully, post.Id), post));
        }

        /// <summary>
        /// Adds a new post to the system.
        /// </summary>               
        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> AddPostAsync([FromBody] Post post)
        {
            var addedPost = await _postsService.AddPostAsync(post);

            return CreatedAtAction(nameof(GetPostByIdAsync), new { postId = addedPost.Id },
                ApiResponse<PostAdminDetailsDto>.CreateSingleItemResponse
             (PostSuccessMessages.PostAddedSuccessfully, addedPost));
        }

        /// <summary>
        /// Updates an existing post.
        /// </summary>               
        [HttpPut("{postId}")]
        [ValidateModel]
        [ValidateId]
        public async Task<IActionResult> UpdatePostAsync(int postId, [FromBody] Post post)
        {
            var updatedPost = await _postsService.UpdatePostAsync(postId, post);

            return Ok(ApiResponse<PostAdminDetailsDto>.CreateSuccessResponse
                (string.Format(PostSuccessMessages.PostUpdatedSuccessfully, postId), updatedPost));
        }

        /// <summary>
        /// Deletes a post by its ID.
        /// </summary>        
        [HttpDelete("{postId}")]
        [ValidateId]
        public async Task<IActionResult> DeletePostAsync(int postId)
        {
            await _postsService.DeletePostAsync(postId);

            return Ok(ApiResponse<Post>.CreateSuccessResponse
                (string.Format(PostSuccessMessages.PostDeletedSuccessfully, postId), postId));
        }
    }
}
