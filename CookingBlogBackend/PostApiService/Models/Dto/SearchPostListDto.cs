namespace PostApiService.Models.Dto
{
    public class SearchPostListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Slug { get; set; } = default!;              
        public string SearchSnippet { get; set; } = default!;
        public string Author { get; set; } = default!;
        public string Category { get; set; } = default!;
    }
}
