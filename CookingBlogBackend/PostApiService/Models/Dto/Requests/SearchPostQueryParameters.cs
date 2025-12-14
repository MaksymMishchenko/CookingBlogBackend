using Microsoft.AspNetCore.Mvc;

namespace PostApiService.Models.Dto.Requests
{
    public class SearchPostQueryParameters : PaginationQueryParameters
    {
        [FromQuery(Name = "queryString")]
        public string QueryString { get; set; } = string.Empty;

    }
}
