using Microsoft.AspNetCore.Mvc;

namespace PostApiService.Models.Dto.Requests
{
    public class PaginationQueryParameters
    {
        [FromQuery(Name = "pageNumber")]
        public int PageNumber { get; set; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; } = 10;
    }
}
