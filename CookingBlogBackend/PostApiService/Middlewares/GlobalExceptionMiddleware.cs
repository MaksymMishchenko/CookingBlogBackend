using Microsoft.Data.SqlClient;
using PostApiService.Infrastructure.Constants;
using PostApiService.Models.Common;
using System.Net;

namespace PostApiService.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, LogMessages.System.UnhandledException, ex.Message, context.Request.Path);

                var (statusCode, message) = ex switch
                {
                    TimeoutException or OperationCanceledException
                        => (HttpStatusCode.RequestTimeout, Global.System.Timeout),

                    SqlException or DbUpdateException
                        => (HttpStatusCode.InternalServerError, Global.System.DbUpdateError),

                    _ => (HttpStatusCode.InternalServerError, Global.System.DatabaseCriticalError)
                };

                await WriteResponseAsync(context, statusCode, message);
            }
        }

        private async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var errorResponse = ApiResponse.CreateErrorResponse(message);
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}
