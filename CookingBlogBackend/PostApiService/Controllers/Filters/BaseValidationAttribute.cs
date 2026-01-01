using Microsoft.AspNetCore.Mvc.Filters;

namespace PostApiService.Controllers.Filters
{
    public abstract class BaseValidationAttribute : ActionFilterAttribute
    {
        protected void HandleInvalidModelState(
            ActionExecutingContext context,
            string generalMessage,
            Func<string, string, string>? formatError = null)
        {
            if (context.ModelState.IsValid) return;

            foreach (var state in context.ModelState.Where(s => s.Value!.Errors.Any()))
            {
                var attemptedValue = state.Value?.AttemptedValue;
                if (!string.IsNullOrEmpty(attemptedValue))
                {
                    Log.Warning(Validation.BindingTypeMismatch, state.Key, attemptedValue);
                }
            }

            var errors = context.ModelState
                .Where(ms => ms.Value!.Errors.Any())
                .ToDictionary(
                    ms => ms.Key,
                    ms => ms.Value!.Errors.Select(e =>
                    {
                        if (formatError != null)
                        {
                            var attemptedValue = context.ModelState[ms.Key]?.AttemptedValue
                                                 ?? Global.Validation.UnknownValue;
                            return formatError(attemptedValue, e.ErrorMessage);
                        }

                        return string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? (e.Exception?.Message ?? Global.Validation.UnexpectedErrorException)
                            : e.ErrorMessage;
                    }).ToArray()
                );

            context.Result = new BadRequestObjectResult(
                ApiResponse.CreateErrorResponse(generalMessage, errors));
        }
    }
}
