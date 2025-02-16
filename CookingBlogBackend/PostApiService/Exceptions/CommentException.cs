namespace PostApiService.Exceptions
{
    public class CommentException : InvalidOperationException
    {
        public CommentException(string message) : base(message) { }
    }

    public class AddCommentFailedException : CommentException
    {
        public int PostId { get; }
        public AddCommentFailedException(int postId)
            : base(string.Format(CommentErrorMessages.AddCommentFailed, postId))
        {
            PostId = postId;
        }
    }
}
