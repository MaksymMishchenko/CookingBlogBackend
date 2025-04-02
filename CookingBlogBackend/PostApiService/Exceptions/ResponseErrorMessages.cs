namespace PostApiService.Exceptions
{
    public static class ResponseErrorMessages
    {
        public const string ValidationFailed = "Validation Failed.";
        public const string TimeoutException = "The request timed out.";
        public const string SqlException = "Database error.";
        public const string DbUpdateException = "Failed to update data.";
        public const string OperationCanceledException = "The operation was canceled.";
        public const string UnexpectedErrorException = "Unexpected error.";
    }
}
