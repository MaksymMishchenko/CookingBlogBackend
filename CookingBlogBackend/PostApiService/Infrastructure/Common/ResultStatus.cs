namespace PostApiService.Infrastructure.Common
{
    public enum ResultStatus
    {
        Success, 
        Created, 
        NoContent,
        NotFound,
        Conflict,
        Unauthorized,
        Forbidden,
        Invalid,
        Error
    }
}
