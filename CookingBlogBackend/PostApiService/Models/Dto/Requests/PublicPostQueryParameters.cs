using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class PublicPostQueryParameters : BasePostQueryParameters
    {        
        [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = Global.Validation.SlugFormat)]
        [StringLength(100, ErrorMessage = Global.Validation.MaxLength)]
        public string? CategorySlug { get; set; }       

        public PublicPostQueryDto ToDto() => new(Search, CategorySlug, PageNumber, PageSize, IsSearchMode);
    }
}
