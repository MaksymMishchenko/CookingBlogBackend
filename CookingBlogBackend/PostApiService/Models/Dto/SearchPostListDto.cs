namespace PostApiService.Models.Dto
{
    public class SearchPostListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;              
        public string SearchSnippet { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }
}
