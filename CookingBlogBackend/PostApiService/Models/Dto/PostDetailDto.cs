namespace PostApiService.Models.Dto
{
    public class PostDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string Author { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public string CategoryName { get; set; } = default!;
    }
}
