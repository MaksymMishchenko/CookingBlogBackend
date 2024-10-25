namespace PostApiService.Dto
{
    public class PostDto
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public DateTime CreateAt { get; set; }
        public string ImageUrl { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Slug { get; set; }
        public List<CommentDto> Comments { get; set; }
    }

    public class CommentDto
    {
        public int CommentId { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public DateTime CreateAt { get; set; }        
    }
}
