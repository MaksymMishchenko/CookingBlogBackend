using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;

namespace PostApiService.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (PostNotFoundException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,                       
                    HttpStatusCode.NotFound,
                    string.Format(ErrorMessages.PostNotFound, ex.PostId));
            }
            catch (PostAlreadyExistException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Conflict,
                    string.Format(ErrorMessages.PostAlreadyExist, ex.Title));
            }
            catch (AddPostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(ErrorMessages.AddPostFailed, ex.Title));
            }
            catch (UpdatePostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(ErrorMessages.UpdatePostFailed, ex.Title));
            }
            catch (DeletePostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(ErrorMessages.DeletePostFailed, ex.PostId));
            }
            catch (TimeoutException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ErrorMessages.TimeoutException);
            }
            catch (SqlException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ErrorMessages.SqlException);
            }
            catch (DbUpdateException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ErrorMessages.DbUpdateException);
            }
            catch (OperationCanceledException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ErrorMessages.OperationCanceledException);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ErrorMessages.UnexpectedErrorException);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context,
            string exMsg,
            HttpStatusCode httpStatusCode,
            string message)
        {
            _logger.LogError(exMsg);

            HttpResponse response = context.Response;

            response.ContentType = "application/json";
            response.StatusCode = (int)httpStatusCode;

            var errorResponse = PostResponse.CreateErrorResponse(message);

            await response.WriteAsJsonAsync(errorResponse);
        }
    }
}
