namespace PostApiService.Models.Dto.Requests
{
    public record AdminPostQueryDto(
       string? SearchTerm,
       int? CategoryId,
       string? AuthorId,
       int PageNumber,
       int PageSize,
       string? SortBy,
       string? SortDirection,
       bool? OnlyActive = null
    );
}
