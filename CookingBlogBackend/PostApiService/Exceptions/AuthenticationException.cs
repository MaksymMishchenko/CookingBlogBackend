namespace PostApiService.Exceptions
{
    public class AuthenticationException : UnauthorizedAccessException
    {
        public AuthenticationException(string message) : base(message) { }
    }
}
