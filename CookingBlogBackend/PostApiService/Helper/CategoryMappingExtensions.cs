using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Helper
{
    public static class CategoryMappingExtensions
    {
        public static CategoryDto ToDto(this Category category) =>
        new(category.Id, category.Name);

        public static Category ToEntity(this CreateCategoryDto dto) =>
            new() { Name = dto.Name };       
        public static void UpdateEntity(this UpdateCategoryDto dto, Category existingCategory)
        {
            existingCategory.Name = dto.Name;
        }
    }
}
