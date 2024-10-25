namespace PostApiService.Models
{
    public class Post
    {
        public int PostId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Content { get; set; }
        public string? Author { get; set; }
        public DateTime CreateAt { get; set; }
        public string ImageUrl { get; set; }

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Slug { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
