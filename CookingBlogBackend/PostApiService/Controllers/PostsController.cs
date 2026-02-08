using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postsService;
        private readonly ICommentService _commentService;

        public PostsController(IPostService postsService, ICommentService commentService)
        {
            _postsService = postsService;
            _commentService = commentService;
        }

        /// <summary>
        /// Retrieves a paginated list of active posts. 
        /// Excludes hidden content and returns the total count of active records.
        /// </summary>         
        [HttpGet]
        public async Task<IActionResult> GetPostsAsync
            ([FromQuery] PostQueryParameters query, CancellationToken ct = default)
        {
            var result = await _postsService.GetPostsPagedAsync
                (query.Search, query.CategorySlug, query.PageNumber, query.PageSize, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Retrieves a paginated list of comments for a specific post.
        /// </summary>
        [HttpGet("{postId:int}/comments")]
        public async Task<IActionResult> GetCommentsByPostIdAsync(
            int postId, [FromQuery] PaginationQueryParameters query, CancellationToken ct = default)
        {
            var result = await _commentService.GetCommentsByPostIdAsync(postId, query.PageNumber, query.PageSize, ct);
            
            return result.ToActionResult();
        }

        /// <summary>
        /// Retrieves details of a specific active post. 
        /// Returns 404 if the post is inactive or does not exist.
        /// </summary>        
        [HttpGet("{category}/{slug}")]
        public async Task<IActionResult> GetActivePostBySlugAsync([FromRoute] PostRequestBySlug dto,
            CancellationToken ct = default)
        {
            var result = await _postsService.GetPostBySlugAsync(dto, ct);

            return result.ToActionResult();
        }
    }
}
