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
        private readonly IAdminPostService _adminPostService;

        public AdminPostsController(IAdminPostService adminPostService)
        {
            _adminPostService = adminPostService;
        }

        /// <summary>
        /// Retrieves a paginated list of posts for the administrative dashboard.
        /// Supports filtering by activity status and includes post statistics (e.g., comment counts).       
        [HttpGet]
        public async Task<IActionResult> GetAdminPostsAsync
            ([FromQuery] AdminPostQueryParameters query, CancellationToken ct = default)
        {
            var result = await _adminPostService.GetAdminPostsPagedAsync(query.ToDto(), ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Retrieves a specific post by its ID.
        /// </summary>        
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPostByIdAsync
            (int id, CancellationToken ct = default)
        {
            var result = await _adminPostService.GetPostByIdAsync(id, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Adds a new post to the system.
        /// </summary>               
        [HttpPost]
        public async Task<IActionResult> AddPostAsync
            ([FromBody] PostCreateDto dto, CancellationToken ct = default)
        {
            var result = await _adminPostService.AddPostAsync(dto, ct);

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
            var result = await _adminPostService.UpdatePostAsync(postId, postDto, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Deletes a post by its ID.
        /// </summary>        
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePostAsync
            (int postId, CancellationToken ct = default)
        {
            var result = await _adminPostService.DeletePostAsync(postId, ct);

            return result.ToActionResult();
        }
    }
}
