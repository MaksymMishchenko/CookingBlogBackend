namespace PostApiService.Exceptions
{
    public class CommentErrorMessages
    {
        public const string CommentNotFound = "Comment with ID {0} was not found.";
        public const string AddCommentFailed = "Failed to add the comment to the post with ID {0}.";
        public const string UpdateCommentFailed = "Failed to update the comment with ID {0}.";
        public const string DeleteCommentFailed = "Failed to delete the comment with ID {0}.";

        public const string CommentCannotBeNull = "Comment cannot be null.";
        public const string MismatchedPostId = "The postId in the request URL does not match the PostId in the comment.";        
        public const string InvalidCommentIdParameter = "Comment ID must be greater than 0.";
        public const string ContentIsRequired = "Comment property 'Content' is required.";

    }
}
