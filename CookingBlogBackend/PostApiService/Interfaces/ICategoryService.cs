using PostApiService.Infrastructure.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface ICategoryService
    {
        public Task<Result<List<CategoryDto>>> GetAllCategoriesAsync(CancellationToken ct = default);
        Task<bool> ExistsAsync(int id, CancellationToken ct = default);
        Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);
        public Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default);
        public Task<Result<CategoryDto>> AddCategoryAsync
            (CreateCategoryDto categoryDto, CancellationToken ct = default);
        public Task<Result<CategoryDto>> UpdateCategoryAsync
            (int categoryId, UpdateCategoryDto categoryDto, CancellationToken ct = default);
        public Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default);
    }
}
