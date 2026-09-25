using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IPublicCategoryService
    {
        Task<Result<List<CategoryDto>>> GetAllCategoriesAsync(CancellationToken ct = default);

        Task<bool> ExistsAsync(int id, CancellationToken ct = default);

        Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);

        Task<string?> GetNameBySlugAsync(string? categorySlug, CancellationToken ct = default);

        Task<string?> GetNameByIdAsync(int? id, CancellationToken ct);
    }
}
