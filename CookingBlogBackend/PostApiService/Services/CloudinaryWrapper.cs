using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using PostApiService.Interfaces;

namespace PostApiService.Services
{
    public class CloudinaryWrapper : ICloudinaryWrapper
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryWrapper(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<ImageUploadResult> UploadAsync(ImageUploadParams parameters)
        {
            return await _cloudinary.UploadAsync(parameters);
        }

        public async Task<DeletionResult> DestroyAsync(DeletionParams parameters)
            => await _cloudinary.DestroyAsync(parameters);
    }
}
