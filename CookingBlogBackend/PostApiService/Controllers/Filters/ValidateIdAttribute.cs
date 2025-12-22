using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;

namespace PostApiService.Controllers.Filters
{
    public class ValidateIdAttribute : ActionFilterAttribute
    {        
        public string InvalidIdErrorMessage { get; set; } = PostErrorMessages.InvalidPostIdParameter;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments)
            {
                if (argument.Value is int id && id <= 0)
                {
                    var errorResponse = ApiResponse.CreateErrorResponse(InvalidIdErrorMessage);

                    context.Result = new BadRequestObjectResult(errorResponse);
                    return;
                }
            }
        }
    }
}
