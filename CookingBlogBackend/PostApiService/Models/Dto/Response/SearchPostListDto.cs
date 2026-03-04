namespace PostApiService.Models.Dto.Response
{
    public record SearchPostListDto
    (
        int Id,
        string Title,
        string Slug,
        string? SearchSnippet,
        string? Description,
        string Author,
        string Category,
        string CategorySlug
    );
}
