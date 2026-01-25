namespace PostApiService.Models.Dto.Response
{
    public record LoggedInUserDto(
        string Token,
        string UserName
    );
}
