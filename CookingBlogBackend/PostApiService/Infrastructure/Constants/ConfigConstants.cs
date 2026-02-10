namespace PostApiService.Infrastructure.Constants
{
    public static class ConfigConstants
    {
        public const string DefaultConnection = "DefaultConnection";        
        public const string JwtSection = "JwtConfiguration";
        public const string HtmlSanitizer = "HtmlSanitizer";
        public const string UnknownIp = "Unknown";
        public const string UnknownUser = "Unknown User";
        public const string RateLimitSection = "RateLimiting";

        public static class Errors
        {
            public const string ConnectionStringNotFound = "Connection string '{0}' is not configured.";
            public const string JwtConfigMissing = "Jwt configuration is missing in the appsettings.json file.";
            public const string ConfigSectionMissing = "Configuration section '{0}' is missing in the appsettings.json file";
        }
    }
}
