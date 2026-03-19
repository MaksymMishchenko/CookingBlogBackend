namespace PostApiService.Models.Dto.Requests
{
    public class CommentCursorParameters
    {
        [FromQuery(Name = "lastId")]
        public int? LastId { get; set; } = null;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; } = 10;
    }
}
