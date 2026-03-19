namespace PostApiService.Models.Dto.Response
{
    public record CommentScrollResponse<T>(
        IEnumerable<T> Items,
        int? LastId,
        bool HasNextPage,
        int TotalCount
    ) : ICursorPagedResult
    {
        public object GetItems() => Items;
    }
}
