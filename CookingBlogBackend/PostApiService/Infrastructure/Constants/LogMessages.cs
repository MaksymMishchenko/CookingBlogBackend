namespace PostApiService.Infrastructure.Constants
{
    public static class LogMessages
    {
        public static class Validation
        {
            public const string InvalidIdValue =
            "Validation Warning: Invalid ID value. Parameter: {ParamName}, Value: {Value}, Path: {Path}";

            public const string PaginationLimitExceeded =
                "Resource Warning: PageSize {RequestedSize} exceeded limit {MaxSize}. IP: {IP}";

            public const string MissingRequestBody =
                "Payload Error: Missing request body. Method: {Method}, Path: {Path}, IP: {IP}";

            public const string BindingTypeMismatch =
                "Binding Error: Property '{Key}' received invalid format '{Value}'";
        }

        public static class Security
        {
            public const string SecurityForbiddenCharacters =
            "Security Alert: Forbidden characters detected. IP: {IP}, Input: {Input}, Path: {Path}";
        }
    }
}
