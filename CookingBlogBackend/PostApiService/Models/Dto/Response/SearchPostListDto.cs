namespace PostApiService.Models.Dto.Response
{
    public record SearchPostListDto
    (
        int Id,
        string Title,
        string Slug,
        string SearchSnippet,
        string Author,
        string Category,
        string CategorySlug
    );
}
