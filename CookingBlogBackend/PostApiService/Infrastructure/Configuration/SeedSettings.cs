namespace PostApiService.Infrastructure.Configuration
{
    public class SeedSettings
    {
        public const string SectionName = "SeedSettings";

        public string AdminUserName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;

        public string ContUserName { get; set; } = string.Empty;
        public string ContEmail { get; set; } = string.Empty;
        public string ContPassword { get; set; } = string.Empty;
    }
}