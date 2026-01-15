namespace PostApiService.Infrastructure.Configuration
{
    public class SanitizerConfiguration
    {
        public RuleSet Comment { get; set; } = new();
        public RuleSet Article { get; set; } = new();

        public class RuleSet
        {
            public List<string> AllowedTags { get; set; } = new();
            public List<string> AllowedAttributes { get; set; } = new();
        }
    }
}
