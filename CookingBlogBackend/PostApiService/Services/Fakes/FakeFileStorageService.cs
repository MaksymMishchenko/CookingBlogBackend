using PostApiService.Interfaces;

namespace PostApiService.Services.Fakes
{
    public class FakeFileStorageService : BaseService, IFileStorageService
    {
        public Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug)
        {            
            var url = $"https://fake-storage.local/posts/{slug}-fake.webp";
            return Task.FromResult(Result<string>.Success(url));
        }
    }
}
