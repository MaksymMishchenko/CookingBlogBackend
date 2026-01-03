using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Controllers.Filters
{
    public class ValidatePaginationParametersAttribute : BaseValidationAttribute
    {
        public string InvalidParametersMessage { get; set; } = Global.Validation.InvalidPageParameters;
        public string SizeExceededMessage { get; set; } = Global.Validation.PageSizeExceeded;
        public int MaxPageSize { get; set; } = 10;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            HandleInvalidModelState(context,
                Global.Validation.ValidationFailed,
                (val, msg) => string.Format(Global.Validation.InvalidNumberFormat, val));

            if (context.Result != null) return;

            if (context.ActionArguments.TryGetValue("query", out var value) &&
                value is PaginationQueryParameters query)
            {
                var logicErrors = new Dictionary<string, string[]>();

                if (query.PageNumber < 1)
                {
                    logicErrors.Add(nameof(query.PageNumber), new[] { InvalidParametersMessage });
                }

                if (query.PageSize < 1)
                {
                    logicErrors.Add(nameof(query.PageSize), new[] { InvalidParametersMessage });
                }
                else if (query.PageSize > MaxPageSize)
                {
                    Log.Warning(Validation.PaginationLimitExceeded,
                        query.PageSize, MaxPageSize, context.HttpContext.Connection.RemoteIpAddress);

                    logicErrors.Add(nameof(query.PageSize), new[] { string.Format(SizeExceededMessage, MaxPageSize) });
                }

                if (logicErrors.Any())
                {
                    context.Result = new BadRequestObjectResult(
                        ApiResponse.CreateErrorResponse(Global.Validation.ValidationFailed, logicErrors));
                    return;
                }
            }
        }
    }
}
