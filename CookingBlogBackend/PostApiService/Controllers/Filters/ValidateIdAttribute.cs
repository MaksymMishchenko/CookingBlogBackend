using Microsoft.AspNetCore.Mvc.Filters;

namespace PostApiService.Controllers.Filters
{
    public class ValidateIdAttribute : BaseValidationAttribute
    {
        public string InvalidIdErrorMessage { get; set; } = Global.Validation.ValidationFailed;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            HandleInvalidModelState(context,
                Global.Validation.ValidationFailed,
                (val, msg) => string.Format(Global.Validation.InvalidNumberFormat, val));

            if (context.Result != null) return;

            foreach (var argument in context.ActionArguments)
            {
                if (argument.Key.ToLower().Contains("id") && argument.Value is int id && id <= 0)
                {
                    Log.Warning(Validation.InvalidIdValue,
                        argument.Key, id, context.HttpContext.Request.Path);

                    var errors = new Dictionary<string, string[]>
                    {
                        { argument.Key, new[] { Global.Validation.InvalidId } }
                    };

                    context.Result = new BadRequestObjectResult(
                        ApiResponse.CreateErrorResponse(InvalidIdErrorMessage, errors));
                    return;
                }
            }
        }
    }
}
