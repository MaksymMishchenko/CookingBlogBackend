namespace PostApiService.Exceptions
{
    public class CommentNotFoundException : KeyNotFoundException
    {
        public int CommentId { get; }
        public CommentNotFoundException(int commentId)
            : base(string.Format(CommentErrorMessages.CommentNotFound, commentId))
        {
            CommentId = commentId;
        }
    }

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

    public class UpdateCommentFailedException : CommentException
    {
        public int CommentId { get; }
        public UpdateCommentFailedException(int commentId)
            : base(string.Format(CommentErrorMessages.UpdateCommentFailed, commentId))
        {
            CommentId = commentId;
        }
    }    
}
