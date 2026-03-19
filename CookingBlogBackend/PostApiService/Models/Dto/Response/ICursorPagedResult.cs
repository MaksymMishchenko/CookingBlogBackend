namespace PostApiService.Models.Dto.Response
{
    public interface ICursorPagedResult
    {
        object GetItems();
        int? LastId { get; }
        bool HasNextPage { get; }
        int TotalCount { get; }
    }
}
