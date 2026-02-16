namespace PostApiService.Models.Dto.Requests
{
    public record PostQueryDto(
        string? SearchTerm,
        string? CategorySlug,
        int PageNumber = 1,
        int PageSize = 10
    );
}
