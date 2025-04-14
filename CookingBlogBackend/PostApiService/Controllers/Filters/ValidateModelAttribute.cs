using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Models;
using PostApiService.Models.Enums;

namespace PostApiService.Controllers.Filters
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public string InvalidErrorMessage { get; set; }
        public ResourceType ErrorResponseType { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(ms => ms.Value?.Errors?.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var errorResponse = CreateErrorResponse(errors);
                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }

        private object CreateErrorResponse(Dictionary<string, string[]> errors)
        {
            return ErrorResponseType switch
            {
                ResourceType.Post => ApiResponse<Post>.CreateErrorResponse(InvalidErrorMessage, errors),
                ResourceType.Comment => ApiResponse<Comment>.CreateErrorResponse(InvalidErrorMessage, errors),
                ResourceType.EditComment => ApiResponse<EditCommentModel>.CreateErrorResponse(InvalidErrorMessage, errors),
                ResourceType.RegisterUser => ApiResponse<RegisterUser>.CreateErrorResponse(InvalidErrorMessage, errors),
                ResourceType.LoginUser => ApiResponse<LoginUser>.CreateErrorResponse(InvalidErrorMessage, errors),
                _ => ApiResponse<object>.CreateErrorResponse("Invalid model state")
            };
        }
    }
}
