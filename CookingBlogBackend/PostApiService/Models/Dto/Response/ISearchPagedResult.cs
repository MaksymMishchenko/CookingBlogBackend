namespace PostApiService.Models.Dto.Response
{
    public interface ISearchPagedResult : IPagedResult
    {       
        string Message { get; }
    }
}
