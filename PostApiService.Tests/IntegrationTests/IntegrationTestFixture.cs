using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostApiService.Tests.IntegrationTests
{
    public class IntegrationTestFixture
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public ApplicationDbContext Context { get; }

        public IntegrationTestFixture()
        {            
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
          
            Context = new ApplicationDbContext(_options);           
            Context.Database.EnsureCreated();
        }

        public ApplicationDbContext CreateContext()
        {           
            return new ApplicationDbContext(_options);
        }

        public void Dispose()
        {
            // Clean up the database after tests
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}
