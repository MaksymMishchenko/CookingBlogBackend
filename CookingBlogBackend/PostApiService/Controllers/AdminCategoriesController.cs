using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Policy = TS.Policies.FullControlPolicy)]
    public class AdminCategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public AdminCategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Retrieves a specific category by its unique identifier.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddСategoryAsync
            ([FromBody] CreateCategoryDto categoryDto, CancellationToken ct = default)
        {
            var result = await _categoryService.AddCategoryAsync(categoryDto, ct);

            return result.ToCreatedResult(nameof(GetCategoryByIdAsync),
                new { id = result.Value?.Id });
        }

        /// <summary>
        /// Updates an existing category's information.
        /// </summary>
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateCategoryAsync
            (int id, [FromBody] UpdateCategoryDto categoryDto, CancellationToken ct = default)
        {
            var result = await _categoryService.UpdateCategoryAsync(id, categoryDto, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Deletes a category from the system.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoryAsync
            (int id, CancellationToken ct = default)
        {
            var result = await _categoryService.DeleteCategoryAsync(id, ct);

            return result.ToActionResult();
        }
    }
}
