namespace PostApiService.Models.Dto.Response
{
    public interface ISearchPagedResult : IPagedResult
    {
        string Query { get; }
        string Message { get; }
    }
}
