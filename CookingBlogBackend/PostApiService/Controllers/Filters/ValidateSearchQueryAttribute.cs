using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Controllers.Filters
{

    namespace PostApiService.Controllers.Filters
    {
        public class ValidateSearchQueryAttribute : ActionFilterAttribute
        {
            private const int MinQueryLength = 3;
            private const int MaxQueryLength = 100;

            public override void OnActionExecuting(ActionExecutingContext context)
            {
                if (context.ActionArguments.TryGetValue("query", out var value) &&
                    value is SearchPostQueryParameters searchQuery)
                {
                    var queryStr = searchQuery.QueryString;

                    if (string.IsNullOrWhiteSpace(queryStr))
                    {
                        context.Result = new BadRequestObjectResult(
                            ApiResponse<object>.CreateErrorResponse("Search query string is required and cannot be empty."));
                        return;
                    }

                    if (queryStr.Length < MinQueryLength)
                    {
                        context.Result = new BadRequestObjectResult(
                            ApiResponse<object>.CreateErrorResponse(
                                $"Query string must be at least {MinQueryLength} characters long."));
                        return;
                    }

                    if (queryStr.Length > MaxQueryLength)
                    {
                        context.Result = new BadRequestObjectResult(
                            ApiResponse<object>.CreateErrorResponse(
                                $"Query string cannot exceed {MaxQueryLength} characters."));
                        return;
                    }
                }

                base.OnActionExecuting(context);
            }
        }
    }

}
