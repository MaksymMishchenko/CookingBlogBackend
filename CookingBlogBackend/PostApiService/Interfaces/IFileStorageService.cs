using PostApiService.Models.Dto.Response;

namespace PostApiService.Interfaces
{
    public interface IFileStorageService
    {
        Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug);
        Task<Result<PostImageUploadResponseDto>> UploadContentImageAsync(IFormFile file, string? alt);
        Task<Result> DeleteByUrlAsync(string url);
    }
}
