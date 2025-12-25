namespace PostApiService.Exceptions
{
    public static class ResponseErrorMessages
    {
        public const string ValidationFailed = "Validation Failed.";
        public const string UnknownValue = "unknown";
        public const string InvalidNumberFormat = "The value '{0}' is not a valid number.";
        public const string RequestBodyRequired = "Request body cannot be empty.";
        public const string SearchQueryForbiddenCharacters =
            "Search query contains forbidden characters. Use only letters, numbers, spaces, dots or hyphens.";
        public const string SearchQueryMustContainLetterOrDigit =
            "Search query must contain at least one letter or digit.";
        public const string TimeoutException = "The request timed out.";
        public const string SqlException = "Database error.";        
        public const string DbUpdateException = "Failed to update data.";
        public const string OperationCanceledException = "The operation was canceled.";
        public const string UnexpectedErrorException = "Unexpected error.";
        
    }
}
