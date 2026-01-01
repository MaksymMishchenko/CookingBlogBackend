using System.Data.Common;

namespace PostApiService.Exceptions
{
    public class PostException : InvalidOperationException
    {
        public PostException(string message) : base(message) { }
    }

    public class PostNotFoundException : KeyNotFoundException
    {
        public int PostId { get; }
        public PostNotFoundException(int postId)
            : base(string.Format(PostM.Errors.PostNotFound, postId))
        {
            PostId = postId;
        }
    }

    public class PostAlreadyExistException : PostException
    {
        public string Title { get; }
        public PostAlreadyExistException(string postTitle)
             : base(string.Format(PostM.Errors.PostAlreadyExist, postTitle))
        {
            Title = postTitle;
        }
    }

    public class AddPostFailedException : InvalidOperationException
    {
        public string Title { get; }
        public AddPostFailedException(string postTitle, DbException ex)
            : base(string.Format(PostM.Errors.AddPostFailed, postTitle), ex)
        {
            Title = postTitle;
        }
    }

    public class UpdatePostFailedException : InvalidOperationException
    {
        public string Title { get; }
        public UpdatePostFailedException(string postTitle, DbException ex)
            : base(string.Format(PostM.Errors.UpdatePostFailed, postTitle), ex)
        {
            Title = postTitle;
        }
    }

    public class DeletePostFailedException : InvalidOperationException
    {
        public int PostId { get; }
        public DeletePostFailedException(int postId, DbException ex)
            : base(string.Format(PostM.Errors.DeletePostFailed, postId), ex)
        {
            PostId = postId;
        }
    }
}
