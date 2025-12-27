using PostApiService.Helper;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using System.Net;

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

        public async Task<Result<bool>> ExistsAsync(int id)
        {
            var exists = await _categoryRepository.AnyAsync(c => c.Id == id);

            return Result<bool>.Success(exists);
        }

        public async Task<Result<List<CategoryDto>>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();

            var dtos = categories.Select(c => c.ToDto()).ToList();

            return Result<List<CategoryDto>>.Success(dtos);
        }

        public async Task<Result<CategoryDto>> GetCategoryByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                return Result<CategoryDto>.NotFound(
                    string.Format(CategoryM.Errors.CategoryNotFound)                    
                );
            }

            return Result<CategoryDto>.Success(category.ToDto());
        }

        public async Task<Result<CategoryDto>> AddCategoryAsync(CreateCategoryDto categoryDto)
        {
            var alreadyExists = await _categoryRepository
                .AnyAsync(c => c.Name == categoryDto.Name);

            if (alreadyExists)
            {
                Log.Information(Categories.CategoryExists, categoryDto.Name);

                return Result<CategoryDto>.Conflict
                   (string.Format(CategoryM.Errors.CategoryAlreadyExists, categoryDto.Name));
            }

            var categoryEntity = CategoryMappingExtensions.ToEntity(categoryDto);

            var result = await _categoryRepository.AddAsync(categoryEntity);
            var responseDto = categoryEntity.ToDto();

            return Result<CategoryDto>.Success(responseDto);
        }

        public async Task<Result<CategoryDto>> UpdateCategoryAsync(int categoryId, UpdateCategoryDto categoryDto)
        {
            var category = await _categoryRepository
                .GetByIdAsync(categoryId);

            if (category == null)
            {
                Log.Warning(Categories.CategoryDoesNotExist, categoryId);

                return Result<CategoryDto>.NotFound(CategoryM.Errors.CategoryNotFound);
            }

            var alreadyExists = await _categoryRepository
                .AnyAsync(c => c.Name == categoryDto.Name && c.Id != categoryId);

            if (alreadyExists)
            {
                Log.Information(Categories.CategoryExists, categoryDto.Name);

                return Result<CategoryDto>.Conflict
                    (string.Format(CategoryM.Errors.CategoryAlreadyExists, categoryDto.Name));
            }

            category.Name = categoryDto.Name;
            await _categoryRepository.UpdateAsync(category);

            var responseDto = category.ToDto();

            return Result<CategoryDto>.Success(responseDto);
        }

        public async Task<Result<bool>> DeleteCategoryAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                Log.Warning(Categories.CategoryDoesNotExist, id);

                return Result<bool>.NotFound(CategoryM.Errors.CategoryNotFound);
            }
          
            var hasPosts = await _postRepository.AnyAsync(c=> c.CategoryId == id);

            if (hasPosts)
            {
                Log.Information(Categories.DeleteBlockedByRelatedPosts, category.Name);
                return Result<bool>.Conflict(CategoryM.Errors.CannotDeleteCategoryWithPosts);
            }            

            await _categoryRepository.DeleteAsync(category);

            return Result<bool>.NoContent();
        }
    }
}
