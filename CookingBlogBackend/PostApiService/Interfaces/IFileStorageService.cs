namespace PostApiService.Interfaces
{
    public interface IFileStorageService
    {
        Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug);
        Task<Result> DeleteByUrlAsync(string url);
    }
}
