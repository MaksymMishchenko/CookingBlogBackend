using PostApiService.Models.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Extensions
{
    public static class ResultExtensions
    {
        private static IActionResult MapErrorResult(Result result)
        {
            var errorResponse = ApiResponse.CreateErrorResponse(result.Message!, errorCode: result.ErrorCode);

            return result.Status switch
            {
                ResultStatus.NotFound => new NotFoundObjectResult(errorResponse),
                ResultStatus.Conflict => new ConflictObjectResult(errorResponse),
                ResultStatus.Invalid => new BadRequestObjectResult(errorResponse),
                ResultStatus.Unauthorized => new UnauthorizedObjectResult(errorResponse),
                ResultStatus.Forbidden => new ObjectResult(errorResponse) { StatusCode = 403 },
                ResultStatus.Error => new ObjectResult(errorResponse) { StatusCode = 500 },
                _ => new ObjectResult(errorResponse) { StatusCode = 500 }
            };
        }

        public static IActionResult ToActionResult(this Result result)
        {
            if (!result.IsSuccess) return MapErrorResult(result);

            if (result.Status == ResultStatus.NoContent)
                return new NoContentResult();

            return new OkObjectResult(ApiResponse.CreateSuccessResponse(result.Message!));
        }

        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (!result.IsSuccess) return MapErrorResult(result);

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

            return new OkObjectResult(ApiResponse<T>.CreateSuccessResponse(result.Message!, result.Value));
        }

        public static IActionResult ToCreatedResult<T>(
            this Result<T> result,
            string actionName,
            object routeValues)
        {
            if (!result.IsSuccess)
            {
                return MapErrorResult(result);
            }

            if (result.Status == ResultStatus.NoContent)
            {
                return new NoContentResult();
            }               

            return result.Status switch
            {
                ResultStatus.Created or ResultStatus.Success => new CreatedAtActionResult(
                    actionName,
                    null,
                    routeValues,
                    ApiResponse<T>.CreateSuccessResponse(result.Message!, result.Value)),
                
                _ => result.ToActionResult()
            };
        }
    }
}
