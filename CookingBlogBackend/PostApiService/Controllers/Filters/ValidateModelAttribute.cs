using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Infrastructure.Constants;
using PostApiService.Models;
using Serilog;

namespace PostApiService.Controllers.Filters
{
    public class ValidateModelAttribute : BaseValidationAttribute
    {
        public string InvalidErrorMessage { get; set; } = ResponseErrorMessages.ValidationFailed;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var bodyArgument = context.ActionArguments.Values
                .FirstOrDefault(arg => arg == null || (!arg.GetType().IsValueType && arg is not string));

            if (bodyArgument == null)
            {
                var method = context.HttpContext.Request.Method;
                var path = context.HttpContext.Request.Path;
                var ip = context.HttpContext.Connection.RemoteIpAddress;

                Log.Warning(LogMessages.MissingRequestBody,
                    method, path, ip);

                context.Result = new BadRequestObjectResult(
                    ApiResponse.CreateErrorResponse(ResponseErrorMessages.RequestBodyRequired));
                return;
            }

            HandleInvalidModelState(context,
                ResponseErrorMessages.ValidationFailed);

            if (context.Result != null) return;
        }
    }
}

