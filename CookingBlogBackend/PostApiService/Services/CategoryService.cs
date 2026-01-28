using Microsoft.IdentityModel.Logging;
using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class CategoryService : BaseResultService, ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IPostRepository _postRepository;

        public CategoryService(IRepository<Category> categoryRepository,
            IPostRepository postRepository)
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
            
            return Success(dtos);
        }

        public async Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, ct);

            if (category == null)
            {
                return NotFound<CategoryDto>(CategoryM.Errors.CategoryNotFound,
                    CategoryM.Errors.CategoryNotFoundCode);
            }

            return Success(category.ToDto());
        }

        public async Task<Result<CategoryDto>> AddCategoryAsync
    (CreateCategoryDto categoryDto, CancellationToken ct = default)
        {
            var alreadyExists = await _categoryRepository
                .AnyAsync(c => c.Name == categoryDto.Name, ct);

            if (alreadyExists)
            {
                Log.Information(Categories.CategoryExists, categoryDto.Name);
                return Conflict<CategoryDto>(
                    string.Format(CategoryM.Errors.CategoryAlreadyExists, categoryDto.Name),
                    CategoryM.Errors.CategoryAlreadyExistsCode);
            }
            
            string source = string.IsNullOrWhiteSpace(categoryDto.Slug)
                ? categoryDto.Name
                : categoryDto.Slug;
            
            string finalSlug = StringHelper.GenerateSlug(source);
            
            categoryDto.Slug = finalSlug;
            
            var categoryEntity = CategoryMappingExtensions.ToEntity(categoryDto);

            await _categoryRepository.AddAsync(categoryEntity, ct);
            await _categoryRepository.SaveChangesAsync(ct);

            return Success(categoryEntity.ToDto(), CategoryM.Success.CategoryAddedSuccessfully);
        }

        public async Task<Result<CategoryDto>> UpdateCategoryAsync
            (int categoryId, UpdateCategoryDto categoryDto, CancellationToken ct = default)
        {
            var category = await _categoryRepository
                .GetByIdAsync(categoryId, ct);

            if (category == null)
            {
                Log.Warning(Categories.CategoryDoesNotExist, categoryId);

                return NotFound<CategoryDto>(CategoryM.Errors.CategoryNotFound,
                    CategoryM.Errors.CategoryNotFoundCode);
            }

            var alreadyExists = await _categoryRepository
                .AnyAsync(c => c.Name == categoryDto.Name && c.Id != categoryId, ct);

            if (alreadyExists)
            {
                Log.Information(Categories.CategoryExists, categoryDto.Name);

                return Conflict<CategoryDto>
                    (string.Format(CategoryM.Errors.CategoryAlreadyExists, categoryDto.Name),
                    CategoryM.Errors.CategoryAlreadyExistsCode);
            }

            categoryDto.UpdateEntity(category);

            await _categoryRepository.UpdateAsync(category, ct);
            await _categoryRepository.SaveChangesAsync(ct);

            var responseDto = category.ToDto();
            
            return Success(responseDto, CategoryM.Success.CategoryUpdatedSuccessfully);
        }

        public async Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, ct);

            if (category == null)
            {
                Log.Warning(Categories.CategoryDoesNotExist, id);
                
                return NotFound(CategoryM.Errors.CategoryNotFound, CategoryM.Errors.CategoryNotFoundCode); 
            }

            var hasPosts = await _postRepository.AnyAsync(c => c.CategoryId == id, ct);

            if (hasPosts)
            {
                Log.Information(Categories.DeleteBlockedByRelatedPosts, category.Name);
                
                return Conflict(CategoryM.Errors.CannotDeleteCategoryWithPosts,
                    CategoryM.Errors.CannotDeleteCategoryWithPostsCode);
            }

            await _categoryRepository.DeleteAsync(category, ct);
            await _categoryRepository.SaveChangesAsync(ct);
           
            return Success(CategoryM.Success.CategoryDeletedSuccessfully);
        }
    }
}
