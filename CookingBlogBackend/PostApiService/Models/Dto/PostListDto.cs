namespace PostApiService.Models.Dto
{
    public class PostListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public string Author { get; set; } = default!;
        public string Category { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = default!;
        public int CommentsCount { get; set; }
    }
}
