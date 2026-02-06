using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class PostQueryParameters : PaginationQueryParameters
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = Global.Validation.LengthRange)]
        public string? Search { get; set; }

        [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = Global.Validation.SlugFormat)]
        [StringLength(100, ErrorMessage = Global.Validation.MaxLength)]
        public string? CategorySlug { get; set; }

        public bool? IsActive { get; set; }
    }
}
