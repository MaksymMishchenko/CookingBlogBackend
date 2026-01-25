namespace PostApiService.Infrastructure.Configuration
{
    public class JwtConfiguration
    {
        public string SecretKey { get; set; } = default!;
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int TokenExpirationMinutes { get; set; } = default!;
    }
}