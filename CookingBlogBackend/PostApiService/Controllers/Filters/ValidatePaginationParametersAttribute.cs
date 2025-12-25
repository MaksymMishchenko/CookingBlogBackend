using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Infrastructure.Constants;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;
using Serilog;

namespace PostApiService.Controllers.Filters
{
    public class ValidatePaginationParametersAttribute : BaseValidationAttribute
    {
        public string InvalidParametersMessage { get; set; } = PostErrorMessages.InvalidPageParameters;
        public string SizeExceededMessage { get; set; } = PostErrorMessages.PageSizeExceeded;
        public int MaxPageSize { get; set; } = 10;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            HandleInvalidModelState(context,
                ResponseErrorMessages.ValidationFailed,
                (val, msg) => string.Format(ResponseErrorMessages.InvalidNumberFormat, val));

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
                    Log.Warning(LogMessages.PaginationLimitExceeded,
                        query.PageSize, MaxPageSize, context.HttpContext.Connection.RemoteIpAddress);

                    logicErrors.Add(nameof(query.PageSize), new[] { string.Format(SizeExceededMessage, MaxPageSize) });
                }

                if (logicErrors.Any())
                {
                    context.Result = new BadRequestObjectResult(
                        ApiResponse.CreateErrorResponse(ResponseErrorMessages.ValidationFailed, logicErrors));
                    return;
                }
            }            
        }
    }
}
