using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Exceptions;
using PostApiService.Infrastructure.Constants;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;
using Serilog;
using System.Text.RegularExpressions;

namespace PostApiService.Controllers.Filters
{
    namespace PostApiService.Controllers.Filters
    {
        public class ValidateSearchQueryAttribute : BaseValidationAttribute
        {
            public int MinQueryLength { get; set; } = 3;
            public int MaxQueryLength { get; set; } = 100;

            public string RequiredMessage { get; set; } = PostErrorMessages.SearchQueryRequired;
            public string TooShortMessage { get; set; } = PostErrorMessages.SearchQueryTooShort;
            public string TooLongMessage { get; set; } = PostErrorMessages.SearchQueryTooLong;

            private static readonly Regex SafeSearchRegex = new Regex(
                @"^[a-zA-Z0-9\s\-\.]+$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public override void OnActionExecuting(ActionExecutingContext context)
            {
                HandleInvalidModelState(context, ResponseErrorMessages.ValidationFailed);

                if (context.Result != null) return;

                if (context.ActionArguments.TryGetValue("query", out var value) &&
                    value is SearchPostQueryParameters searchQuery)
                {
                    var queryStr = searchQuery.QueryString?.Trim();
                    string? error = null;

                    if (string.IsNullOrWhiteSpace(queryStr))
                    {
                        error = RequiredMessage;
                    }
                    else if (queryStr.Length < MinQueryLength)
                    {
                        error = string.Format(TooShortMessage, MinQueryLength);
                    }
                    else if (queryStr.Length > MaxQueryLength)
                    {
                        error = string.Format(TooLongMessage, MaxQueryLength);
                    }
                    else if (!queryStr.Any(char.IsLetterOrDigit))
                    {
                        error = ResponseErrorMessages.SearchQueryMustContainLetterOrDigit;
                    }
                    else if (!SafeSearchRegex.IsMatch(queryStr))
                    {
                        Log.Warning(LogMessages.SecurityForbiddenCharacters,
                            context.HttpContext.Connection.RemoteIpAddress, queryStr);

                        error = ResponseErrorMessages.SearchQueryForbiddenCharacters;
                    }

                    if (error != null)
                    {
                        var errors = new Dictionary<string, string[]> { { nameof(searchQuery.QueryString), [error] } };
                        context.Result = new BadRequestObjectResult(
                            ApiResponse.CreateErrorResponse(ResponseErrorMessages.ValidationFailed, errors));
                        return;
                    }
                }
            }
        }
    }
}
