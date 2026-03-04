using System.Text.Json.Serialization;

namespace PostApiService.Models.Dto.Response
{
    public record SearchPostListDto
    (
        int Id,
        string Title,
        string Slug,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? SearchSnippet,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Description,
        string Author,
        string Category,
        string CategorySlug
    );
}
