using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface ICategoryService
    {
        Task<Result<List<CategoryDto>>> GetAllCategoriesAsync(CancellationToken ct = default);

        Task<bool> ExistsAsync(int id, CancellationToken ct = default);

        Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);

        Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default);

        Task<string?> GetNameBySlugAsync(string? categorySlug, CancellationToken ct = default);

        Task<string?> GetNameByIdAsync(int? id, CancellationToken ct);

        Task<Result<CategoryDto>> AddCategoryAsync
            (CreateCategoryDto categoryDto, CancellationToken ct = default);

        Task<Result<CategoryDto>> UpdateCategoryAsync
            (int categoryId, UpdateCategoryDto categoryDto, CancellationToken ct = default);

        Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default);
    }
}
