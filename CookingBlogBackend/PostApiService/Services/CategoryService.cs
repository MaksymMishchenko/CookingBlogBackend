using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Post> _postRepository;

        public CategoryService(IRepository<Category> categoryRepository, IRepository<Post> postRepository)
        {
            _categoryRepository = categoryRepository;
            _postRepository = postRepository;
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        {
            return await _categoryRepository.AnyAsync(c => c.Id == id, ct);            
        }

        public async Task<Result<List<CategoryDto>>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            var categories = await _categoryRepository.GetAllAsync(ct);

            var dtos = categories
                .OrderBy(c => c.Id)
                .Select(c => c.ToDto()).ToList();

            return Result<List<CategoryDto>>.Success(dtos);
        }

        public async Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, ct);

            if (category == null)
            {
                return Result<CategoryDto>.NotFound(
                    string.Format(CategoryM.Errors.CategoryNotFound)
                );
            }

            return Result<CategoryDto>.Success(category.ToDto());
        }

        public async Task<Result<CategoryDto>> AddCategoryAsync
            (CreateCategoryDto categoryDto, CancellationToken ct = default)
        {
            var alreadyExists = await _categoryRepository
                .AnyAsync(c => c.Name == categoryDto.Name, ct);

            if (alreadyExists)
            {
                Log.Information(Categories.CategoryExists, categoryDto.Name);

                return Result<CategoryDto>.Conflict
                   (string.Format(CategoryM.Errors.CategoryAlreadyExists, categoryDto.Name));
            }

            var categoryEntity = CategoryMappingExtensions.ToEntity(categoryDto);

            await _categoryRepository.AddAsync(categoryEntity, ct);
            await _categoryRepository.SaveChangesAsync(ct);

            var responseDto = categoryEntity.ToDto();

            return Result<CategoryDto>.Success(responseDto, CategoryM.Success.CategoryAddedSuccessfully);
        }

        public async Task<Result<CategoryDto>> UpdateCategoryAsync
            (int categoryId, UpdateCategoryDto categoryDto, CancellationToken ct = default)
        {
            var category = await _categoryRepository
                .GetByIdAsync(categoryId, ct);

            if (category == null)
            {
                Log.Warning(Categories.CategoryDoesNotExist, categoryId);

                return Result<CategoryDto>.NotFound(CategoryM.Errors.CategoryNotFound);
            }

            var alreadyExists = await _categoryRepository
                .AnyAsync(c => c.Name == categoryDto.Name && c.Id != categoryId, ct);

            if (alreadyExists)
            {
                Log.Information(Categories.CategoryExists, categoryDto.Name);

                return Result<CategoryDto>.Conflict
                    (string.Format(CategoryM.Errors.CategoryAlreadyExists, categoryDto.Name));
            }

            categoryDto.UpdateEntity(category);

            await _categoryRepository.UpdateAsync(category, ct);
            await _categoryRepository.SaveChangesAsync(ct);

            var responseDto = category.ToDto();

            return Result<CategoryDto>.Success(responseDto, CategoryM.Success.CategoryUpdatedSuccessfully);
        }

        public async Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, ct);

            if (category == null)
            {
                Log.Warning(Categories.CategoryDoesNotExist, id);

                return Result<bool>.NotFound(CategoryM.Errors.CategoryNotFound);
            }

            var hasPosts = await _postRepository.AnyAsync(c => c.CategoryId == id, ct);

            if (hasPosts)
            {
                Log.Information(Categories.DeleteBlockedByRelatedPosts, category.Name);
                return Result.Conflict(CategoryM.Errors.CannotDeleteCategoryWithPosts);
            }

            await _categoryRepository.DeleteAsync(category, ct);
            await _categoryRepository.SaveChangesAsync(ct);

            return Result.Success(CategoryM.Success.CategoryDeletedSuccessfully);
        }
    }
}
