using PostApiService.Controllers.Filters;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = TS.Policies.FullControlPolicy)]
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
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            var result = await _categoryService.GetAllCategoriesAsync(ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Retrieves a specific category by its unique identifier.
        /// </summary>
        [HttpGet("{id}")]
        [ValidateId]
        public async Task<IActionResult> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id, ct);

            return result.ToActionResult();
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        [HttpPost]
        [ValidateModel]
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
        [ValidateId]
        [ValidateModel]
        public async Task<IActionResult> UpdateCategoryAsync
            (int id, [FromBody] UpdateCategoryDto categoryDto, CancellationToken ct = default)
        {
            var result = await _categoryService.UpdateCategoryAsync(id, categoryDto, ct);

            return result.ToActionResult(CategoryM.Success.CategoryUpdatedSuccessfully);
        }

        /// <summary>
        /// Deletes a category from the system.
        /// </summary>
        [HttpDelete("{id}")]
        [ValidateId]
        public async Task<IActionResult> DeleteCategoryAsync
            (int id, CancellationToken ct = default)
        {
            var result = await _categoryService.DeleteCategoryAsync(id, ct);

            return result.ToActionResult(CategoryM.Success.CategoryDeletedSuccessfully);
        }
    }
}
