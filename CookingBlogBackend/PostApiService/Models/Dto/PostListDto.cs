namespace PostApiService.Models.Dto
{
    public class PostListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CommentsCount { get; set; }
    }
}
