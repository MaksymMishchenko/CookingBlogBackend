using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class PublicCategoryService : BaseResultService, IPublicCategoryService
    {
        private readonly ICategoryRepository _categoryRepository;        

        public PublicCategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;            
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        {
            return await _categoryRepository.AnyAsync(c => c.Id == id, ct);
        }

        public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
        {
            return await _categoryRepository.AnyAsync(c => c.Slug == slug, ct);
        }

        public async Task<Result<List<CategoryDto>>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            var categories = await _categoryRepository.GetAllAsync(ct);

            var dtos = categories
                .OrderBy(c => c.Id)
                .Select(c => c.ToDto()).ToList();

            return Success(dtos);
        }

        public Task<string?> GetNameBySlugAsync(string? categorySlug, CancellationToken ct = default)
        {
            return _categoryRepository.GetNameBySlugAsync(categorySlug, ct);
        }

        public async Task<string?> GetNameByIdAsync(int? id, CancellationToken ct)
        {
            return await _categoryRepository.GetNameByIdAsync(id, ct);
        }
    }
}
