using Microsoft.Extensions.DependencyInjection;

namespace PostApiService.Tests.Helper
{
    public static class DataSeedingExtensions
    {
        public static async Task SeedBlogDataAsync(
            this IServiceProvider services,
            IEnumerable<Post> posts,
            IEnumerable<Category> categories)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (categories.Any()) await dbContext.Categories.AddRangeAsync(categories);
            if (posts.Any()) await dbContext.Posts.AddRangeAsync(posts);

            await dbContext.SaveChangesAsync();
        }

        public static async Task SeedCategoriesAsync(
            this IServiceProvider services,
            IEnumerable<Category> categories)
        {
            using (var scope = services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Categories.AddRangeAsync(categories);

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
