namespace PostApiService.Models.Dto.Response
{
    public record PagedResult<T>(List<T> Items, int TotalCount, int pageNumber, int pageSize);
}
