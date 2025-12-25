using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Infrastructure.Constants;
using PostApiService.Models;
using Serilog;

namespace PostApiService.Controllers.Filters
{
    public class ValidateIdAttribute : BaseValidationAttribute
    {
        public string InvalidIdErrorMessage { get; set; } = PostErrorMessages.InvalidPostIdParameter;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            HandleInvalidModelState(context,
                ResponseErrorMessages.ValidationFailed,
                (val, msg) => string.Format(ResponseErrorMessages.InvalidNumberFormat, val));

            if (context.Result != null) return;

            foreach (var argument in context.ActionArguments)
            {
                if (argument.Key.ToLower().Contains("id") && argument.Value is int id && id <= 0)
                {
                    Log.Warning(LogMessages.InvalidIdValue,
                        argument.Key, id, context.HttpContext.Request.Path);

                    var errors = new Dictionary<string, string[]>
                    {
                        { argument.Key, new[] { ResponseErrorMessages.ValidationFailed } }
                    };

                    context.Result = new BadRequestObjectResult(
                        ApiResponse.CreateErrorResponse(InvalidIdErrorMessage, errors));
                    return;
                }
            }            
        }
    }
}
