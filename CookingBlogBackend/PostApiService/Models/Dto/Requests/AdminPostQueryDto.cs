namespace PostApiService.Models.Dto.Requests
{
    public record AdminPostQueryDto(
       string? SearchTerm,
       int? CategoryId,
       int PageNumber,
       int PageSize,
       bool? OnlyActive = null
    );
}
