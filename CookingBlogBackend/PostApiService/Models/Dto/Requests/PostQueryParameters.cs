using Microsoft.AspNetCore.Mvc;

namespace PostApiService.Models.Dto.Requests
{
    public class PostQueryParameters
    {
        [FromQuery(Name = "pageNumber")]
        public int PageNumber { get; set; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; } = 10;

        [FromQuery(Name = "commentPageNumber")]
        public int CommentPageNumber { get; set; } = 1;

        [FromQuery(Name = "commentsPerPage")]
        public int CommentsPerPage { get; set; } = 10;

        [FromQuery(Name = "includeComments")]
        public bool IncludeComments { get; set; } = true;
    }
}
