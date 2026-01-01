using Microsoft.AspNetCore.Mvc.Filters;

namespace PostApiService.Controllers.Filters
{
    public class ValidateModelAttribute : BaseValidationAttribute
    {
        public string InvalidErrorMessage { get; set; } = Global.Validation.ValidationFailed;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var bodyArgument = context.ActionArguments.Values
                .FirstOrDefault(arg => arg == null || (!arg.GetType().IsValueType && arg is not string));

            if (bodyArgument == null)
            {
                var method = context.HttpContext.Request.Method;
                var path = context.HttpContext.Request.Path;
                var ip = context.HttpContext.Connection.RemoteIpAddress;

                Log.Warning(Validation.MissingRequestBody,
                    method, path, ip);

                context.Result = new BadRequestObjectResult(
                    ApiResponse.CreateErrorResponse(Global.Validation.RequestBodyRequired));
                return;
            }

            HandleInvalidModelState(context,
                Global.Validation.ValidationFailed);

            if (context.Result != null) return;
        }
    }
}

