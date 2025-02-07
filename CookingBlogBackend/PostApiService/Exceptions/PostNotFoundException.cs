namespace PostApiService.Exceptions
{
    public class PostNotFoundException : KeyNotFoundException
    {
        public PostNotFoundException(int postId)
            : base(string.Format(ErrorMessages.PostNotFound, postId)) { }
    }
}
