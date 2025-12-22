using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Controllers.Filters
{
    public class ValidatePaginationParametersAttribute : ActionFilterAttribute
    {
        public string InvalidParametersMessage { get; set; } = PostErrorMessages.InvalidPageParameters;
        public string SizeExceededMessage { get; set; } = PostErrorMessages.PageSizeExceeded;
        public int MaxPageSize { get; set; } = 10;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.TryGetValue("query", out var value) &&
                value is PaginationQueryParameters query)
            {
                if (query.PageNumber < 1 || query.PageSize < 1)
                {
                    context.Result = new BadRequestObjectResult(ApiResponse.CreateErrorResponse(
                        InvalidParametersMessage));
                    return;
                }

                if (query.PageSize > MaxPageSize)
                {
                    context.Result = new BadRequestObjectResult(ApiResponse.CreateErrorResponse(
                        string.Format(SizeExceededMessage, MaxPageSize)));
                    return;
                }
            }
        }
    }
}
