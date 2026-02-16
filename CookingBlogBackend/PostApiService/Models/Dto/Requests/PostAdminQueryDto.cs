namespace PostApiService.Models.Dto.Requests
{
    public record PostAdminQueryDto(
       string? SearchTerm,
       string? CategorySlug,
       int PageNumber,
       int PageSize,
       bool? OnlyActive = null
    ) : PostQueryDto(SearchTerm, CategorySlug, PageNumber, PageSize);
}
