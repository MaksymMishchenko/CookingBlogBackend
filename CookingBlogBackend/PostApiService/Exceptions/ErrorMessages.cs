namespace PostApiService.Exceptions
{
    public static class ErrorMessages
    {
        public const string PostNotFound = "Post with ID {0} was not found.";
        public const string PostAlreadyExist = "Post with title {0} already exists.";
        public const string AddPostFailed = "Failed to add post with title '{0}'.";
        public const string UpdatePostFailed = "Failed to update post with title '{0}'.";
        public const string DeletePostFailed = "Failed to delete post with title '{0}'.";
        public const string OperationCanceledException = "The request was cancelled.";
        public const string SqlException = "Database error while processing data.";
        public const string TimeoutException = "The request timed out.";
        public const string UnexpectedErrorException = "An error occurred while processing your request.";
        public const string DbUpdateException = "Database error occurred while processing data.";

        public const string InvalidPageParameters = "Parameters must be greater than 0.";
        public const string PageSizeExceeded = "Page size or comments per page exceeds the allowed maximum.";
        public const string NoPostsFound = "No posts found for the requested page.";
        public const string PostCannotBeNull = "Post cannot be null.";
        public const string ValidationFailed = "Validation failed.";
        public const string InvalidPostIdParameter = "Post ID must be greater than 0.";
        public const string InvalidPostOrId = "Post cannot be null, and ID should be greater than 0.";
    }
}
