using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Controllers.Filters
{
    public class ValidatePostQueryParametersAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.TryGetValue("query", out var value) &&
                value is PostQueryParameters query)
            {
                if (query.PageNumber < 1 || query.PageSize < 1)
                {
                    context.Result = new BadRequestObjectResult(ApiResponse<Post>.CreateErrorResponse(
                        PostErrorMessages.InvalidPageParameters));
                    return;
                }

                if (query.PageSize > 10 || query.CommentsPerPage > 10)
                {
                    context.Result = new BadRequestObjectResult(ApiResponse<Post>.CreateErrorResponse(
                        PostErrorMessages.PageSizeExceeded));
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
