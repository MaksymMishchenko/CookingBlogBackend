using CloudinaryDotNet.Actions;

namespace PostApiService.Interfaces
{
    public interface ICloudinaryWrapper
    {
        Task<ImageUploadResult> UploadAsync(ImageUploadParams parameters);
    }
}