using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PostApiService.Contexts
{
    namespace PostApiService.Contexts
    {
        public class AppIdentityDbContext : IdentityDbContext
        {
            public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }
        }
    }
}
