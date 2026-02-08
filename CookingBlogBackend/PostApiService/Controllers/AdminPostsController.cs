using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/admin/posts")]
    [Authorize(Policy = TS.Policies.FullControlPolicy)]
    public class AdminPostsController : ControllerBase
    {
        private readonly IPostService _postsService;

        public AdminPostsController(IPostService postsService)
        {
            _postsService = postsService;
        }

        /// <summary>
        /// Retrieves a paginated list of posts for the administrative dashboard.
        /// Supports filtering by activity status and includes post statistics (e.g., comment counts).       
        [HttpGet]
        public async Task<IActionResult> GetAdminPostsAsync
            ([FromQuery] PostAdminQueryParameters query, CancellationToken ct = default)
        {
            var result = await _postsService.GetAdminPostsPagedAsync
                (query.Search, query.CategorySlug, query.OnlyActive, query.PageNumber, query.PageSize, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Retrieves a specific post by its ID.
        /// </summary>        
        [HttpGet("{id:int}")]
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
        public async Task<IActionResult> DeletePostAsync
            (int postId, CancellationToken ct = default)
        {
            var result = await _postsService.DeletePostAsync(postId, ct);

            return result.ToActionResult();
        }
    }
}
