namespace PostApiService.Models.Dto.Response
{
    public record PagedSearchResult<T>(
        string Query,
        IEnumerable<T> Items,
        int TotalSearchCount,
        int PageNumber,
        int PageSize,
        string? Message) : ISearchPagedResult
    {
        public object GetItems() => Items;
        public int TotalCount => TotalSearchCount;
        string ISearchPagedResult.Query => Query;
        string ISearchPagedResult.Message => Message!;
    }
}
