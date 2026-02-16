namespace PostApiService.Models.Dto.Response
{
    public record PagedSearchResult<T>(        
        IEnumerable<T> Items,
        AppliedFiltersDto AppliedFilters,
        int TotalSearchCount,
        int PageNumber,
        int PageSize,
        string? Message) : ISearchPagedResult
    {
        public object GetItems() => Items;
        public int TotalCount => TotalSearchCount;
        AppliedFiltersDto IPagedResult.AppliedFilters => AppliedFilters;
        string ISearchPagedResult.Message => Message!;
    }
}
