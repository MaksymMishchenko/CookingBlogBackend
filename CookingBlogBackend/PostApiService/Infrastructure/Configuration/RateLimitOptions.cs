using System.ComponentModel.DataAnnotations;

namespace PostApiService.Infrastructure.Configuration
{
    public class RateLimitOptions
    {        
        public const string PolicyName = "comment_limit_policy";

        [Range(1, 100, ErrorMessage = Global.Validation.LengthRange)]
        public int PermitLimit { get; set; }

        [Range(1, 60, ErrorMessage = Global.Validation.LengthRange)]
        public int WindowMinutes { get; set; }
        
        public static class Errors
        {
            public const string LimitExceeded = "You have reached the limit of actions ({0} per {1} minute).";
            public const string ErrorCode = "LIMIT_EXCEEDED";
        }
    }
}
