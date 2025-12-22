using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Controllers.Filters
{
    namespace PostApiService.Controllers.Filters
    {
        public class ValidateSearchQueryAttribute : ActionFilterAttribute
        {           
            public int MinQueryLength { get; set; } = 3;
            public int MaxQueryLength { get; set; } = 100;
            
            public string RequiredMessage { get; set; } = PostErrorMessages.SearchQueryRequired;
            public string TooShortMessage { get; set; } = PostErrorMessages.SearchQueryTooShort;
            public string TooLongMessage { get; set; } = PostErrorMessages.SearchQueryTooLong;

            public override void OnActionExecuting(ActionExecutingContext context)
            {
                if (context.ActionArguments.TryGetValue("query", out var value) &&
                    value is SearchPostQueryParameters searchQuery)
                {
                    var queryStr = searchQuery.QueryString?.Trim();

                    if (string.IsNullOrWhiteSpace(queryStr))
                    {
                        context.Result = new BadRequestObjectResult(
                            ApiResponse.CreateErrorResponse(RequiredMessage));
                        return;
                    }

                    if (queryStr.Length < MinQueryLength)
                    {
                        context.Result = new BadRequestObjectResult(
                            ApiResponse.CreateErrorResponse(
                                string.Format(TooShortMessage, MinQueryLength)));
                        return;
                    }

                    if (queryStr.Length > MaxQueryLength)
                    {
                        context.Result = new BadRequestObjectResult(
                            ApiResponse.CreateErrorResponse(
                                string.Format(TooLongMessage, MaxQueryLength)));
                        return;
                    }
                }
            }
        }
    }
}
