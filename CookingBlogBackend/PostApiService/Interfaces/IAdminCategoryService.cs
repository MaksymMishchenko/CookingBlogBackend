using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IAdminCategoryService
    {
        Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default);

        Task<Result<CategoryDto>> AddCategoryAsync
            (CreateCategoryDto categoryDto, CancellationToken ct = default);

        Task<Result<CategoryDto>> UpdateCategoryAsync
            (int categoryId, UpdateCategoryDto categoryDto, CancellationToken ct = default);

        Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default);
    }
}
