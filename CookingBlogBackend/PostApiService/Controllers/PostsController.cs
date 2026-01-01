using PostApiService.Controllers.Filters;
using PostApiService.Controllers.Filters.PostApiService.Controllers.Filters;
using PostApiService.Interfaces;
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
        public async Task<IActionResult> GetPostsWithTotalPostCountAsync
            ([FromQuery] PostQueryParameters query, CancellationToken ct = default)
        {
            var result = await _postsService.GetPostsWithTotalPostCountAsync
                (query.PageNumber, query.PageSize, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Searches posts by query string and returns a paginated list of results 
        /// along with the total count of matched records.                  
        [HttpGet("search")]
        [AllowAnonymous]
        [ValidateSearchQuery]
        [ValidatePaginationParameters]
        public async Task<IActionResult> SearchPostsWithTotalCountAsync
            ([FromQuery] SearchPostQueryParameters query, CancellationToken ct = default)
        {
            var result = await _postsService.SearchPostsWithTotalCountAsync
                (query.QueryString, query.PageNumber, query.PageSize, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Retrieves a specific post by its ID.
        /// </summary>        
        [HttpGet("{id}")]
        [ValidateId]
        public async Task<IActionResult> GetPostByIdAsync
            (int id, CancellationToken ct = default)
        {
            var result = await _postsService.GetPostByIdAsync(id, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Adds a new post to the system.
        /// </summary>               
        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> AddPostAsync
            ([FromBody] PostCreateDto dto, CancellationToken ct = default)
        {
            var result = await _postsService.AddPostAsync(dto, ct);

            return result.ToCreatedResult(nameof(GetPostByIdAsync),
                new { id = result.Value?.Id });
        }

        /// <summary>
        /// Updates an existing post.
        /// </summary>               
        [HttpPut("{postId}")]
        [ValidateId]
        [ValidateModel]
        public async Task<IActionResult> UpdatePostAsync
            (int postId, [FromBody] PostUpdateDto postDto, CancellationToken ct = default)
        {
            var result = await _postsService.UpdatePostAsync(postId, postDto, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Deletes a post by its ID.
        /// </summary>        
        [HttpDelete("{postId}")]
        [ValidateId]
        public async Task<IActionResult> DeletePostAsync
            (int postId, CancellationToken ct = default)
        {
            var result = await _postsService.DeletePostAsync(postId, ct);

            return result.ToActionResult();
        }
    }
}
