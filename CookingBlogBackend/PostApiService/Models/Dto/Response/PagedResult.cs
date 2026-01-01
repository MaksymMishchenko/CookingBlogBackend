namespace PostApiService.Models.Dto.Response
{
    public record PagedResult<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize) : IPagedResult
    {
        public object GetItems() => Items;        
    }
}
