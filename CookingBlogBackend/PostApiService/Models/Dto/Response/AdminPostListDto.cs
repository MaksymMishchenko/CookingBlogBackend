namespace PostApiService.Models.Dto.Response
{
    public record AdminPostListDto(
        int Id,
        string Title,
        string Author,
        string Slug,
        string CategorySlug,
        int? CategoryId,
        string CategoryName,
        DateTime CreatedAt,
        bool IsActive       
    );
}
