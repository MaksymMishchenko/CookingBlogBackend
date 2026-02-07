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
        /// Retrieves a paginated list of active posts. 
        /// Excludes hidden content and returns the total count of active records.
        /// </summary>         
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostsAsync
            ([FromQuery] PostQueryParameters query, CancellationToken ct = default)
        {
            var result = await _postsService.GetPostsPagedAsync
                (query.Search, query.CategorySlug, query.PageNumber, query.PageSize, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Retrieves a paginated list of posts for the administrative dashboard.
        /// Supports filtering by activity status and includes post statistics (e.g., comment counts).       
        [HttpGet("admin")]        
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
        /// Retrieves details of a specific active post. 
        /// Returns 404 if the post is inactive or does not exist.
        /// </summary>        
        [HttpGet("{category}/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActivePostBySlugAsync([FromRoute] PostRequestBySlug dto,
            CancellationToken ct = default)
        {
            var result = await _postsService.GetActivePostBySlugAsync(dto, ct);

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
