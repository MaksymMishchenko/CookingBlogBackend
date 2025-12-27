using PostApiService.Infrastructure.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface ICategoryService
    {
        public Task<Result<List<CategoryDto>>> GetAllCategoriesAsync();
        public Task<Result<bool>> ExistsAsync(int id);
    }
}
