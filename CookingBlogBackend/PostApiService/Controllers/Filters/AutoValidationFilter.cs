using Microsoft.AspNetCore.Mvc.Filters;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using System.Text.RegularExpressions;

namespace PostApiService.Controllers.Filters
{
    // TODO: Migrate to FluentValidation when ActionArguments grows (Refactor Tech Debt #33)
    public class AutoValidationFilter : ActionFilterAttribute
    {
        private static readonly Regex SafeSearchRegex = new Regex(
            @"^[a-zA-Z0-9\s\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                HandleInvalidModelState(context);
                return;
            }

            foreach (var arg in context.ActionArguments)
            {
                var key = arg.Key;
                var value = arg.Value;

                if (key.ToLower().Contains("id") && value is int idValue && idValue <= 0)
                {
                    ReturnBadRequest(context, key, Global.Validation.InvalidId);
                    return;
                }

                if (value is PaginationQueryParameters pg)
                {
                    if (pg.PageNumber < 1 || pg.PageSize < 1)
                    {
                        ReturnBadRequest(context, nameof(pg.PageNumber), Global.Validation.InvalidPageParameters);
                        return;
                    }
                    if (pg.PageSize > 10)
                    {
                        ReturnBadRequest(context, nameof(pg.PageSize), string.Format(Global.Validation.PageSizeExceeded, 10));
                        return;
                    }
                }

                if (value is SearchPostQueryParameters search)
                {
                    var error = ValidateSearchString(search.QueryString);
                    if (error != null)
                    {
                        ReturnBadRequest(context, nameof(search.QueryString), error);
                        return;
                    }
                }
            }
        }

        private void HandleInvalidModelState(ActionExecutingContext context)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value!.Errors.Any())
                .ToDictionary(
                    ms => ms.Key,
                    ms => ms.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new BadRequestObjectResult(
                ApiResponse.CreateErrorResponse(Global.Validation.ValidationFailed, errors));
        }

        private void ReturnBadRequest(ActionExecutingContext context, string key, string message)
        {
            var errors = new Dictionary<string, string[]> { { key, [message] } };
            context.Result = new BadRequestObjectResult(
                ApiResponse.CreateErrorResponse(Global.Validation.ValidationFailed, errors));
        }

        private string? ValidateSearchString(string? query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Global.Validation.SearchQueryRequired;
            if (query.Length < 3) return string.Format(Global.Validation.SearchQueryTooShort, 3);
            if (query.Length > 100) return string.Format(Global.Validation.SearchQueryTooLong, 100);
            if (!query.Any(char.IsLetterOrDigit)) return Global.Validation.SearchQueryMustContainLetterOrDigit;
            if (!SafeSearchRegex.IsMatch(query)) return Global.Validation.SearchQueryForbiddenCharacters;

            return null;
        }
    }
}