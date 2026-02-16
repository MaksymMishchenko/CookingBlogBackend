namespace PostApiService.Models.Dto.Requests
{
    public class PostAdminQueryParameters : PostQueryParameters
    {
        public bool? OnlyActive { get; set; }

        public new PostAdminQueryDto ToDto() => new(Search, CategorySlug, PageNumber, PageSize, OnlyActive);
    }
}
