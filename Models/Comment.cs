namespace PostApiService.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }
    }
}
