namespace PostApiService.Models.Dto.Requests
{
    public class PostAdminQueryParameters : PostQueryParameters
    {
        public bool? OnlyActive { get; set; }
    }
}
