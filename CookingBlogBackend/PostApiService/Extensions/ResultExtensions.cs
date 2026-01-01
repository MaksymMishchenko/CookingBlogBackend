using PostApiService.Models.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Extensions
{
    public static class ResultExtensions
    {
        private static IActionResult MapErrorResult<T>(Result<T> result)
        {
            return result.Status switch
            {
                ResultStatus.NotFound => new NotFoundObjectResult(ApiResponse.CreateErrorResponse(result.Message, errorCode: result.ErrorCode)),
                ResultStatus.Conflict => new ConflictObjectResult(ApiResponse.CreateErrorResponse(result.Message, errorCode: result.ErrorCode)),
                ResultStatus.Invalid => new BadRequestObjectResult(ApiResponse.CreateErrorResponse(result.Message)),
                ResultStatus.Unauthorized => new UnauthorizedObjectResult(ApiResponse.CreateErrorResponse(result.Message ?? "Unauthorized")),
                _ => new StatusCodeResult(500)
            };
        }

        public static IActionResult ToActionResult<T>(this Result<T> result, string? message = null)
        {
            if (!result.IsSuccess) return MapErrorResult(result);

            if (result.Status == ResultStatus.NoContent && message == null)
            {
                return new NoContentResult();
            }

            if (result.Value is IPagedResult pageData)
            {
                var items = (IEnumerable<object>)pageData.GetItems();

                if (result.Value is ISearchPagedResult search)
                {
                    return new OkObjectResult(ApiResponse<IEnumerable<object>>.CreatePaginatedResponse(
                     items,
                     search.PageNumber,
                     search.PageSize,
                     search.TotalCount,
                     search.Message,
                     search.Query));
                }

                return new OkObjectResult(ApiResponse<IEnumerable<object>>.CreatePaginatedResponse(
                    items,
                    pageData.PageNumber,
                    pageData.PageSize,
                    pageData.TotalCount));
            }

            var finalMessage = message ?? result.Message;

            return new OkObjectResult(ApiResponse<T>.CreateSuccessResponse(finalMessage, result.Value));
        }

        public static IActionResult ToCreatedResult<T>(
            this Result<T> result,
            string actionName,
            object routeValues,
            string? message = null)
        {
            if (!result.IsSuccess)
            {
                return MapErrorResult(result);
            }

            var finalMessage = message ?? result.Message;

            return result.Status switch
            {
                ResultStatus.Success => new CreatedAtActionResult(
                    actionName,
                    null,
                    routeValues,
                    ApiResponse<T>.CreateSuccessResponse(finalMessage, result.Value)),

                _ => result.ToActionResult()
            };
        }
    }
}
