namespace PostApiService.Models.Dto.Response
{
    public interface IPagedResult
    {
        object GetItems();
        AppliedFiltersDto? AppliedFilters { get; }
        int PageNumber { get; }
        int PageSize { get; }
        int TotalCount { get; }        
    }
}
