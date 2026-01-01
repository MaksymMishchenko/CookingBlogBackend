namespace PostApiService.Exceptions
{
    public class PostNotFoundException : KeyNotFoundException
    {
        public int PostId { get; }
        public PostNotFoundException(int postId)
            : base(string.Format(PostM.Errors.PostNotFound, postId))
        {
            PostId = postId;
        }
    }
}
