namespace PostApiService.Models.Dto.Requests
{
    public record AdminPostQueryDto(
       string? SearchTerm,
       string? CategorySlug,
       int PageNumber,
       int PageSize,
       bool? OnlyActive = null
    );
}
