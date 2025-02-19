namespace PostApiService.Exceptions
{
    public class CommentErrorMessages
    {
        public const string CommentNotFound = "Comment with ID {0} was not found.";
        public const string AddCommentFailed = "Failed to add the comment to the post with ID {0}.";
        public const string UpdateCommentFailed = "Failed to update the comment with ID {0}.";
    }
}
