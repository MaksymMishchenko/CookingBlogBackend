namespace PostApiService.Models.Dto.Responses
{
    public class PostAdminDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string Author { get; set; } = default!;
        public string ImageUrl { get; set; } = default!;
        public string Slug { get; set; } = default!;

        public string MetaTitle { get; set; } = default!;
        public string MetaDescription { get; set; } = default!;

        public int CategoryId { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
