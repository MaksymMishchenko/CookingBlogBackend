namespace PostApiService.Exceptions
{
    public class PostException : InvalidOperationException
    {
        public PostException(string message) : base(message) { }
    }

    public class PostNotFoundException : PostException
    {
        public int PostId { get; }
        public PostNotFoundException(int postId)
            : base(string.Format(ErrorMessages.PostNotFound, postId))
        {
            PostId = postId;
        }
    }

    public class PostAlreadyExistException : PostException
    {
        public string Title { get; }
        public PostAlreadyExistException(string postTitle)
             : base(string.Format(ErrorMessages.PostAlreadyExist, postTitle))
        {
            Title = postTitle;
        }
    }

    public class AddPostFailedException : InvalidOperationException
    {
        public string Title { get; }
        public AddPostFailedException(string postTitle)
            : base(string.Format(ErrorMessages.AddPostFailed, postTitle))
        {
            Title = postTitle;
        }
    }

    public class UpdatePostFailedException : InvalidOperationException
    {
        public string Title { get; }
        public UpdatePostFailedException(string postTitle)
            : base(string.Format(ErrorMessages.UpdatePostFailed, postTitle))
        {
            Title = postTitle;
        }
    }

    public class DeletePostFailedException : InvalidOperationException
    {
        public int PostId { get; }
        public DeletePostFailedException(int postId)
            : base(string.Format(ErrorMessages.DeletePostFailed, postId))
        {
            PostId = postId;
        }
    }
}
