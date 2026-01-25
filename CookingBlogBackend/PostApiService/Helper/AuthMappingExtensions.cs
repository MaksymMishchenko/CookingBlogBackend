using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Helper
{
    public static class AuthMappingExtensions
    {       
        public static IdentityUser ToEntity(this RegisterUserDto dto)
        {
            return new IdentityUser
            {
                UserName = dto.UserName,
                Email = dto.Email
            };
        }
       
        public static RegisteredUserDto ToRegisteredDto(this IdentityUser user)
        {
            return new RegisteredUserDto(
                user.Id!,
                user.UserName!,
                user.Email!
            );
        }

        public static LoggedInUserDto ToLoggedInUserDto(this string token, string userName)
        {
            return new LoggedInUserDto(
               token,
               userName
            );
        }
    }
}
