using PostApiService.Infrastructure.Constants;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.TypeSafe;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/admin/image")]
    [Authorize(Policy = TS.Policies.FullControlPolicy)]
    public class AdminImageController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;

        public AdminImageController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        [HttpPost]
        [RequestSizeLimit(StorageConstants.Limits.MaxImageRequestSizeBytes)]
        public async Task<IActionResult> UploadContentImage(
            [FromForm] UploadContentImageDto dto,
            CancellationToken ct = default)
        {
            var result = await _fileStorageService.UploadContentImageAsync(dto.ContentImage, dto.Alt, ct);

            return result.ToActionResult();
        }
    }
}
