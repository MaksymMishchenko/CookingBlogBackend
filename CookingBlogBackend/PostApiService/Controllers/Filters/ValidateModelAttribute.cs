using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Models;

namespace PostApiService.Controllers.Filters
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public string InvalidErrorMessage { get; set; } = ResponseErrorMessages.ValidationFailed;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var bodyArgument = context.ActionArguments.Values.FirstOrDefault();

            if (bodyArgument == null && context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(
                    ApiResponse.CreateErrorResponse(ResponseErrorMessages.RequestBodyRequired));
                return;
            }

            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(ms => ms.Value!.Errors.Any())
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? (e.Exception?.Message ?? ResponseErrorMessages.UnexpectedErrorException)
                            : e.ErrorMessage)
                        .ToArray()
                    );

                var errorResponse = ApiResponse.CreateErrorResponse(InvalidErrorMessage, errors);
                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }
    }
}

