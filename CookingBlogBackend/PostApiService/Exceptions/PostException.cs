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
            : base(string.Format(PostErrorMessages.PostNotFound, postId))
        {
            PostId = postId;
        }
    }

    public class PostAlreadyExistException : PostException
    {
        public string Title { get; }
        public string Slug { get; }
        public PostAlreadyExistException(string message, string title, string slug)
             : base(message)
        {
            Title = title;
            Slug = slug;
        }
    }
    public class ValidationFailedException : PostException
    {        
        public IDictionary<string, string[]> Errors { get; }

        public ValidationFailedException(string message, IDictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }
        
        public ValidationFailedException(string propertyName, string errorMessage)
            : base("One or more validation errors occurred.")
        {
            Errors = new Dictionary<string, string[]>
            {
                { propertyName, new[] { errorMessage } }
            };
        }
    }

    public class AddPostFailedException : InvalidOperationException
    {
        public string Title { get; }
        public AddPostFailedException(string postTitle, DbException ex)
            : base(string.Format(PostErrorMessages.AddPostFailed, postTitle), ex)
        {
            Title = postTitle;
        }
    }

    public class UpdatePostFailedException : InvalidOperationException
    {
        public string Title { get; }
        public UpdatePostFailedException(string postTitle, DbException ex)
            : base(string.Format(PostErrorMessages.UpdatePostFailed, postTitle), ex)
        {
            Title = postTitle;
        }
    }

    public class DeletePostFailedException : InvalidOperationException
    {
        public int PostId { get; }
        public DeletePostFailedException(int postId, DbException ex)
            : base(string.Format(PostErrorMessages.DeletePostFailed, postId), ex)
        {
            PostId = postId;
        }
    }
}
