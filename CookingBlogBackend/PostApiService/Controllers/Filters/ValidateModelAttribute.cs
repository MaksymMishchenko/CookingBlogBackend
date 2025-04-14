using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Models;
using PostApiService.Models.Enums;

namespace PostApiService.Controllers.Filters
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public string InvalidIdErrorMessage { get; set; }
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
                ResourceType.Post => ApiResponse<Post>.CreateErrorResponse(InvalidIdErrorMessage),
                ResourceType.Comment => ApiResponse<Comment>.CreateErrorResponse(InvalidIdErrorMessage),
                ResourceType.EditComment => ApiResponse<EditCommentModel>.CreateErrorResponse(InvalidIdErrorMessage),
                ResourceType.RegisterUser => ApiResponse<RegisterUser>.CreateErrorResponse(InvalidIdErrorMessage),
                ResourceType.LoginUser => ApiResponse<LoginUser>.CreateErrorResponse(InvalidIdErrorMessage),
                _ => ApiResponse<object>.CreateErrorResponse("Invalid model state")
            };
        }
    }
}
