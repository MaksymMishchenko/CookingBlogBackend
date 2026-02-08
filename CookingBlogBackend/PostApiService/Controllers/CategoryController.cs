using PostApiService.Interfaces;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Retrieves a list of all culinary categories.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            var result = await _categoryService.GetAllCategoriesAsync(ct);

            return result.ToActionResult();
        }
    }
}
