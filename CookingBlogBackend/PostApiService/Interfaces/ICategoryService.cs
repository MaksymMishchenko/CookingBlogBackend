using PostApiService.Models.Dto.Responses;

namespace PostApiService.Interfaces
{
    public interface ICategoryService
    {
        public Task<List<CategoryDto>> GetAllCategoriesAsync();
        public Task<bool> ExistsAsync(int id);      
    }
}
