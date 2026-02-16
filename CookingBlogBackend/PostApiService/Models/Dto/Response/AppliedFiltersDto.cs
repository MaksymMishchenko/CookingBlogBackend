using System.Text.Json.Serialization;

namespace PostApiService.Models.Dto.Response
{
    public record AppliedFiltersDto(
        [property: JsonPropertyName("search")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Search,

        [property: JsonPropertyName("category")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? CategoryName,

        [property: JsonPropertyName("onlyActive")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        bool? OnlyActive = null
    );
}
