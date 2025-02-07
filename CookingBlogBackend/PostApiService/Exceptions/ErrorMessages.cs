namespace PostApiService.Exceptions
{
    public static class ErrorMessages
    {
        public const string PostNotFound = "Post with ID {0} was not found.";
        public const string PostAlreadyExist = "Post with title {0} already exists.";
        public const string AddPostFailed = "Failed to add post with title '{0}'.";
        public const string UpdatePostFailed = "Failed to update post with title '{0}'.";
    }
}
