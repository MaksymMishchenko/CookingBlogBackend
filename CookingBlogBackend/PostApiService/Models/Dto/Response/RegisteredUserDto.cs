namespace PostApiService.Models.Dto.Response
{
    public record RegisteredUserDto(
      string Id,
      string UserName,
      string Email
    );
}
