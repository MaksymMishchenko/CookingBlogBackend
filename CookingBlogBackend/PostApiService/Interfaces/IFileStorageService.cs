namespace PostApiService.Interfaces
{
    public interface IFileStorageService
    {
        Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug);
        Task<Result<List<string>>> SavePostContentImagesAsync(IReadOnlyList<IFormFile> files, string slug);
        Task<Result> DeleteByUrlAsync(string url);
        Task<Result> DeleteByUrlsAsync(IEnumerable<string> urls);
    }
}
