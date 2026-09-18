namespace PostApiService.Models.Dto.Requests
{
    public class AdminPostQueryParameters : BasePostQueryParameters
    {
        public bool? OnlyActive { get; set; }

        public int? CategoryId { get; set; }

        public string? SortBy { get; set; }

        public string? SortDirection { get; set; }

        public AdminPostQueryDto ToDto() => new(Search, CategoryId, PageNumber, PageSize, SortBy, SortDirection, OnlyActive);
    }
}
