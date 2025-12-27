using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Helper
{
    public static class CategoryMappingExtensions
    {
        public static CategoryDto ToDto(this Category category)
        {
            return new CategoryDto(
                category.Id,
                category.Name
            );
        }

        public static Category ToEntity(this CategoryDto dto)
        {
            return new Category
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        public static Category ToEntity(this CreateCategoryDto dto)
        {
            return new Category
            {
                Name = dto.Name               
            };
        }

        public static Category ToEntity(this UpdateCategoryDto dto)
        {
            return new Category
            {
                Name = dto.Name
            };
        }
    }
}
