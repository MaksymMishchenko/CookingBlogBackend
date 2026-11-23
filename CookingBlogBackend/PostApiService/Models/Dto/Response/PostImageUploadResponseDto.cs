namespace PostApiService.Models.Dto.Response
{
    public record PostImageUploadResponseDto(
    Guid Id,
    string Url,
    string Alt
    );
}
