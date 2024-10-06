namespace PostApiService.Models
{
    public class Post
    {
        public int PostId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }

        public DateTime CreateAt { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
