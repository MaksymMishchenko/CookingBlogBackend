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

        public static class System
        {
            public const string DatabaseCriticalError = "Database critical failure. Details: {Message}";
        }

        public static class Categories
        {
            public const string CategoryExists = "Add category failed: Name already exists. Name: {Name}";
            public const string CategoryDoesNotExist =
                "Update Category Failed: Category with ID {Id} not found in database. Input Name: {Name}";
            public const string DeleteBlockedByRelatedPosts = "Delete blocked: Category {Name} has active posts";

        }
    }
}
