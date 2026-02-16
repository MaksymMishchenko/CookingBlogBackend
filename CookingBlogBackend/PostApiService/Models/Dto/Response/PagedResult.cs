namespace PostApiService.Models.Dto.Response
{
    public record PagedResult<T>(
        IEnumerable<T> Items,        
        int TotalCount,
        int PageNumber,
        int PageSize,
        AppliedFiltersDto? AppliedFilters = null) : IPagedResult
    {
        public object GetItems() => Items;
        AppliedFiltersDto? IPagedResult.AppliedFilters => AppliedFilters ?? new AppliedFiltersDto(null, null);
    }
}
