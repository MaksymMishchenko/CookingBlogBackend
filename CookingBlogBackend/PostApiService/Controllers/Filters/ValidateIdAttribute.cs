using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Models;
using PostApiService.Models.Enums;

namespace PostApiService.Controllers.Filters
{
    public class ValidateIdAttribute : ActionFilterAttribute
    {
        public string InvalidIdErrorMessage { get; set; }
        public ResourceType ErrorResponseType { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments)
            {
                if (argument.Value is int id && id <= 0)
                {
                    var errorResponse = CreateErrorResponse();

                    context.Result = new BadRequestObjectResult(errorResponse);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }

        private object CreateErrorResponse()
        {
            return ErrorResponseType switch
            {
                ResourceType.Comment => ApiResponse<Comment>.CreateErrorResponse(InvalidIdErrorMessage),
                ResourceType.Post => ApiResponse<Post>.CreateErrorResponse(InvalidIdErrorMessage),
                _ => ApiResponse<object>.CreateErrorResponse("Invalid ID")
            };
        }
    }
}
