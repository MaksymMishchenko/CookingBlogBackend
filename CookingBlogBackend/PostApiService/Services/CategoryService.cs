using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Dto.Responses;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class CategoryService : ICategoryService
    {
        private IRepository<Category> _categoryRepository;
        public CategoryService(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<bool> ExistsAsync(int id)
        {            
            return await _categoryRepository.AnyAsync(c => c.Id == id);
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Select(c => new CategoryDto(c.Id, c.Name)).ToList();
        }
    }
}
