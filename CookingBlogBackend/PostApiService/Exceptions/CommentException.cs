using System.Data.Common;

namespace PostApiService.Exceptions
{
    public class CommentException : InvalidOperationException
    {
        public CommentException(string message, DbException ex) : base(message, ex) { }
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
