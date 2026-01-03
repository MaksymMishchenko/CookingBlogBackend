namespace PostApiService.Models.Dto.Response
{
    public interface IPagedResult
    {
        object GetItems(); 
        int PageNumber { get; }
        int PageSize { get; }
        int TotalCount { get; }        
    }
}
