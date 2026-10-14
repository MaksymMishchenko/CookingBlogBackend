namespace PostApiService.Interfaces
{
    public interface IImageService
    {
        Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug);
    }
}
