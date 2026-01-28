using PostApiService.Infrastructure.Services;

namespace PostApiService.Services
{
    public abstract class BaseService: BaseResultService
    {
        protected readonly IWebContext WebContext;

        protected BaseService(IWebContext webContext) => WebContext = webContext;        
    }
}

