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

        public static class Posts
        {
            public const string Created = "Post '{Title}' created successfully with ID {Id}";
            public const string Updated = "Post '{Title}' updated successfully with ID {Id}";
            public const string Deleted = "Post with ID {Id} deleted successfully";
            public const string AlreadyExists = "Post with title '{0}' or slug '{1}' already exists.";
            public const string NotFound = "Admin Post Search: Post with ID {PostId} was not found.";
            public const string CategoryExists = "Add category failed: Name already exists. Name: {Name}";
            public const string CategoryNotFound = "Post creation failed: Category {CategoryId} not found";
            public const string CategoryDoesNotExist =
                "Update Category Failed: Category with ID {Id} not found in database. Input Name: {Name}";
            public const string DeleteBlockedByRelatedPosts = "Delete blocked: Category {Name} has active posts";

        }

        // 
    }
}
