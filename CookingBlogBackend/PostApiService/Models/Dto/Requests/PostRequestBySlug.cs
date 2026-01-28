using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class PostRequestBySlug
    {
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = Global.Validation.SlugFormat)]
        public string Category { get; init; } = default!;

        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = Global.Validation.SlugFormat)]
        public string Slug { get; init; } = default!;
    }
}
