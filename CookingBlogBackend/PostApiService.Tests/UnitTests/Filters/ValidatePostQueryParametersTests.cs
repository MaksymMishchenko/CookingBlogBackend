using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Controllers.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class ValidatePostQueryParametersTests
    {
        [Fact]
        public void Sets_BadRequest_When_PageNumber_Less_Than_1()
        {
            // Arrange
            var query = new PostQueryParameters { PageNumber = 0, PageSize = 5 };
            var context = CreateContext(query);
            var attribute = new ValidatePostQueryParametersAttribute();

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            Assert.IsType<BadRequestObjectResult>(context.Result);
            var result = context.Result as BadRequestObjectResult;
            Assert.Equal(PostErrorMessages.InvalidPageParameters, ((ApiResponse<Post>)result.Value).Message);
        }

        [Fact]
        public void Sets_BadRequest_When_PageSize_More_Than_10()
        {
            var query = new PostQueryParameters { PageNumber = 1, PageSize = 11 };
            var context = CreateContext(query);
            var attribute = new ValidatePostQueryParametersAttribute();

            attribute.OnActionExecuting(context);

            Assert.IsType<BadRequestObjectResult>(context.Result);
            var result = context.Result as BadRequestObjectResult;
            Assert.Equal(PostErrorMessages.PageSizeExceeded, ((ApiResponse<Post>)result.Value).Message);
        }

        [Fact]
        public void Sets_BadRequest_When_CommentsPerPage_More_Than_10()
        {
            var query = new PostQueryParameters { PageNumber = 1, PageSize = 5, CommentsPerPage = 11 };
            var context = CreateContext(query);
            var attribute = new ValidatePostQueryParametersAttribute();

            attribute.OnActionExecuting(context);

            Assert.IsType<BadRequestObjectResult>(context.Result);
            var result = context.Result as BadRequestObjectResult;
            Assert.Equal(PostErrorMessages.PageSizeExceeded, ((ApiResponse<Post>)result.Value).Message);
        }

        [Fact]
        public void Does_Not_Set_Result_When_Parameters_Are_Valid()
        {
            var query = new PostQueryParameters { PageNumber = 1, PageSize = 5, CommentsPerPage = 5 };
            var context = CreateContext(query);
            var attribute = new ValidatePostQueryParametersAttribute();

            attribute.OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        private ActionExecutingContext CreateContext(PostQueryParameters query)
        {
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            };

            var actionArguments = new Dictionary<string, object>
            {
                { "query", query }
            };

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null
            );
        }
    }
}
