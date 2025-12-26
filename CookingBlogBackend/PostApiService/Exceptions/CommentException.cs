using System.Data.Common;

namespace PostApiService.Exceptions
{
    public class CommentNotFoundException : KeyNotFoundException
    {
        public int CommentId { get; }
        public CommentNotFoundException(int commentId)
            : base(string.Format(CommentM.Errors.CommentNotFound, commentId))
        {
            CommentId = commentId;
        }
    }

    public class CommentException : InvalidOperationException
    {
        public CommentException(string message, DbException ex) : base(message, ex) { }
    }

    public class AddCommentFailedException : CommentException
    {
        public int PostId { get; }
        public AddCommentFailedException(int postId, DbException ex)
            : base(string.Format(CommentM.Errors.AddCommentFailed, postId), ex)
        {
            PostId = postId;
        }
    }

    public class UpdateCommentFailedException : CommentException
    {
        public int CommentId { get; }
        public UpdateCommentFailedException(int commentId, DbException ex)
            : base(string.Format(CommentM.Errors.UpdateCommentFailed, commentId), ex)
        {
            CommentId = commentId;
        }
    }
    public class DeleteCommentFailedException : CommentException
    {
        public int CommentId { get; }
        public DeleteCommentFailedException(int commentId, DbException ex)
            : base(string.Format(CommentM.Errors.DeleteCommentFailed, commentId), ex)
        {
            CommentId = commentId;
        }
    }
}
