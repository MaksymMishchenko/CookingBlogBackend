using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace PostApiService.Tests.Helper
{
    public static class BaseTestFixtureExtensions
    {
        public static async Task<TResult> ExecuteInScopeAsync<TResult>(
            this BaseTestFixture fixture,
            Func<ApplicationDbContext, Task<TResult>> action)
        {
            using var scope = fixture.Services!.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await action(context);
        }

        public static async Task AssertEntityExistsAsync<TEntity>(
            this BaseTestFixture fixture,
            Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var exists = await fixture.ExecuteInScopeAsync(async db =>
                await db.Set<TEntity>().AnyAsync(predicate));

            Assert.True(exists, $"Entity of type {typeof(TEntity).Name} was expected to exist, but was not found.");
        }

        public static async Task AssertEntityDeletedAsync<TEntity>(
            this BaseTestFixture fixture,
            Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var exists = await fixture.ExecuteInScopeAsync(async db =>
                await db.Set<TEntity>().AnyAsync(predicate));

            Assert.False(exists, $"Entity of type {typeof(TEntity).Name} still exists in the database, but it was expected to be deleted.");
        }               
    }
}
