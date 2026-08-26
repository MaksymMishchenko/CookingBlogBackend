namespace PostApiService.Models.Dto.Requests
{
    public class AdminPostQueryParameters : BasePostQueryParameters
    {
        public bool? OnlyActive { get; set; }
        public string? CategorySlug { get; set; }

        public AdminPostQueryDto ToDto() => new(Search, CategorySlug, PageNumber, PageSize, OnlyActive);
    }
}
