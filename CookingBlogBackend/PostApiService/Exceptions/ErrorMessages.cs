namespace PostApiService.Exceptions
{
    public static class ErrorMessages
    {
        public const string PostNotFound = "Post with ID {0} was not found.";
        public const string PostAlreadyExist = "Post with title {0} already exists.";
        public const string PostNotSaved = "Post with title {0} not saved.";
    }
}
