using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using PostApiService.Controllers.Filters;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Tests.Filters
{
    public class AutoValidationFilterTests
    {
        private readonly AutoValidationFilter _filter;

        public AutoValidationFilterTests()
        {
            _filter = new AutoValidationFilter();
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            // Arrange            
            var context = CreateContext(new Dictionary<string, object?> { { "id", 0 } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.InvalidId, response.Errors!["id"][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldHandleInvalidModelState_Automatically()
        {
            // Arrange
            var context = CreateContext(new Dictionary<string, object?>());
            context.ModelState.AddModelError("Title", "Title is required");

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Contains("Title is required", response.Errors!["Title"]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenPageNumberOrSizeIsInvalid()
        {
            // Arrange
            var queryParams = new PaginationQueryParameters { PageNumber = 0, PageSize = 5 };
            var context = CreateContext(new Dictionary<string, object?> { { "query", queryParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.InvalidPageParameters, response.Errors!["PageNumber"][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            // Arrange
            var queryParams = new PaginationQueryParameters { PageNumber = 1, PageSize = 15 };
            var context = CreateContext(new Dictionary<string, object?> { { "query", queryParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(string.Format(Global.Validation.PageSizeExceeded, 10), response.Errors!["PageSize"][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenSearchContainsForbiddenCharacters()
        {
            // Arrange
            var queryParams = new PublicPostQueryParameters { Search = "SQL' injection--" };
            var context = CreateContext(new Dictionary<string, object?> { { "query", queryParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.SearchQueryForbiddenCharacters, response.Errors!["Search"][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenSearchHasNoLettersOrDigits()
        {
            // Arrange
            var queryParams = new PublicPostQueryParameters { Search = "---" };
            var context = CreateContext(new Dictionary<string, object?> { { "query", queryParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.SearchQueryMustContainLetterOrDigit, response.Errors!["Search"][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenSortByIsInvalid()
        {
            // Arrange
            var queryParams = new AdminPostQueryParameters { SortBy = "invalid_field" };
            var context = CreateContext(new Dictionary<string, object?> { { "query", queryParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var response = Assert.IsType<ApiResponse>(result.Value);
            Assert.Equal(Global.Validation.InvalidSortField, response.Errors!["SortBy"][0]);
        }

        [Fact]
        public void OnActionExecuting_ShouldPass_WhenSortParametersAreValidOrNull()
        {
            // Arrange
            var queryParams = new AdminPostQueryParameters { SortBy = "title", SortDirection = "asc" };
            var context = CreateContext(new Dictionary<string, object?> { { "query", queryParams } });

            // Act
            _filter.OnActionExecuting(context);

            // Assert            
            Assert.Null(context.Result);
        }

        private ActionExecutingContext CreateContext(Dictionary<string, object?> actionArguments)
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary()
            );

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                Substitute.For<Controller>()
            );
        }
    }
}