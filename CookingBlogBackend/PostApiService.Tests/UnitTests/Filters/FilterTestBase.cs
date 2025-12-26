using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using System.Net;

namespace PostApiService.Tests.UnitTests.Filters
{
    public abstract class FilterTestBase
    {
        protected ActionExecutingContext CreateContext(
            Dictionary<string, object?> arguments,
            string method = "POST",
            string path = "/api/test")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
            httpContext.Request.Method = method;
            httpContext.Request.Path = path;

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                Substitute.For<ActionDescriptor>(),
                new ModelStateDictionary()
            );

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                arguments,
                Substitute.For<Controller>()
            );
        }
    }
}
