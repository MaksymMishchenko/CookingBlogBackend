using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public abstract class BasePostQueryParameters : PaginationQueryParameters
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = Global.Validation.LengthRange)]
        public string? Search { get; set; }

        public bool IsSearchMode { get; set; } = false;
    }
}
