namespace PostApiService.Models.Dto.Requests
{
    public class AdminPostQueryParameters : PublicPostQueryParameters
    {
        public bool? OnlyActive { get; set; }

        public new AdminPostQueryDto ToDto() => new(Search, CategorySlug, PageNumber, PageSize, OnlyActive);
    }
}
