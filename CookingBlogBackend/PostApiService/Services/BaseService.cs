using PostApiService.Infrastructure.Services;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Linq.Expressions;

namespace PostApiService.Services
{
    public abstract class BaseService : BaseResultService
    {
        protected readonly IWebContext? WebContext;
        protected BaseService() { }

        protected BaseService(IWebContext webContext) => WebContext = webContext;

        protected async Task<PagedResult<TDto>> GetPagedDataAsync<TEntity, TDto>(
             IQueryable<TEntity> query,
             AppliedFilters appliedFilters,
             int pageNumber,
             int pageSize,
             Expression<Func<TEntity, TDto>> projection,
             CancellationToken ct)
        {
            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(projection)
                .ToListAsync(ct);

            var filtersDto = new AppliedFiltersDto(appliedFilters.SearchTerm, appliedFilters.CategoryName);

            return new PagedResult<TDto>(items, totalCount, pageNumber, pageSize, filtersDto);
        }
    }
}

