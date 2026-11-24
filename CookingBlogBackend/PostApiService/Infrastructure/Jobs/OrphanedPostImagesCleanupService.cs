using PostApiService.Infrastructure.Constants;
using PostApiService.Interfaces;
using PostApiService.Repositories;

namespace PostApiService.Infrastructure.Jobs
{
    public class OrphanedPostImagesCleanupService : BackgroundService
    {
        private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
        private static readonly TimeSpan OrphanMinAge = TimeSpan.FromHours(24);
        private readonly IServiceScopeFactory _scopeFactory;

        public OrphanedPostImagesCleanupService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information(LogMessages.OrphanedImagesCleanup.Started);

            using var timer = new PeriodicTimer(RunInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, LogMessages.OrphanedImagesCleanup.CleanupFailed);
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Log.Information(LogMessages.OrphanedImagesCleanup.Stopped);
        }

        internal async Task CleanupAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var postImageRepository = scope.ServiceProvider.GetRequiredService<IPostImageRepository>();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            var cutoffUtc = DateTime.UtcNow.Subtract(OrphanMinAge);
            var orphanedImages = await postImageRepository.GetOrphanedImagesAsync(cutoffUtc, ct);

            if (orphanedImages.Count == 0)
            {
                Log.Information(LogMessages.OrphanedImagesCleanup.NoneFound, cutoffUtc);
                return;
            }

            var deletedImages = new List<PostImage>();

            foreach (var image in orphanedImages)
            {
                ct.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(image.Url))
                {
                    var deleteResult = await fileStorageService.DeleteByUrlAsync(image.Url);
                    if (!deleteResult.IsSuccess)
                    {
                        Log.Warning(
                            LogMessages.OrphanedImagesCleanup.StorageDeleteFailed,
                            image.Id, image.Url);
                        continue;
                    }
                }

                deletedImages.Add(image);
            }

            if (deletedImages.Count > 0)
            {
                await postImageRepository.DeleteRangeAsync(deletedImages, ct);
                await postImageRepository.SaveChangesAsync(ct);
            }

            Log.Information(
                LogMessages.OrphanedImagesCleanup.Finished,
                deletedImages.Count, orphanedImages.Count);
        }
    }
}
