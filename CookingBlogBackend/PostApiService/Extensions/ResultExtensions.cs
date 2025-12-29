namespace PostApiService.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result, string? message = null)
        {
            return result.Status switch
            {
                ResultStatus.Success => new OkObjectResult(ApiResponse<T>.CreateSuccessResponse(message, result.Value)),

                ResultStatus.NoContent => message != null
                ? new OkObjectResult(ApiResponse<T>.CreateSuccessResponse(message))
                : new NoContentResult(),

                ResultStatus.NotFound => new NotFoundObjectResult(ApiResponse.CreateErrorResponse(result.ErrorMessage)),

                ResultStatus.Conflict => new ConflictObjectResult(ApiResponse.CreateErrorResponse(result.ErrorMessage)),

                ResultStatus.Invalid => new BadRequestObjectResult(ApiResponse.CreateErrorResponse(result.ErrorMessage)),

                _ => new StatusCodeResult(500)
            };
        }

        public static IActionResult ToCreatedResult<T>(
            this Result<T> result,
            string actionName,
            object routeValues,
            string? message = null)
        {
            return result.Status switch
            {
                ResultStatus.Success => new CreatedAtActionResult(
                    actionName,
                    null,
                    routeValues,
                    ApiResponse<T>.CreateSuccessResponse(message, result.Value)),

                _ => result.ToActionResult()
            };
        }
    }
}
