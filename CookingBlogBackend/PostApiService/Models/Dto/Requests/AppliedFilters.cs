namespace PostApiService.Models.Dto.Requests
{
    public record AppliedFilters(
        string? SearchTerm,
        string? CategoryName
    );
}
