namespace PostApiService.Models.Dto.Requests
{
    public class PostQueryParameters : PaginationQueryParameters
    {
        public bool? isActive { get; set; }
    }
}
