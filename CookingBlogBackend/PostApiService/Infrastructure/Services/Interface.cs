namespace PostApiService.Infrastructure.Services
{
    public interface IWebContext
    {
        string IpAddress { get; }
        string? UserId { get; }
        string UserName { get; }
        bool IsAdmin { get; }
    }
}
