using Microsoft.Data.SqlClient;
using PostApiService.Exceptions;
using PostApiService.Infrastructure.Constants;
using PostApiService.Models.Common;
using System.Net;
using System.Security.Authentication;

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
            catch (UserNotFoundException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.Unauthorized,
                    Auth.LoginM.Errors.InvalidCredentials);
            }
            catch (UserAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.Conflict,
                    Auth.Registration.Errors.UsernameAlreadyExists);
            }
            catch (EmailAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.Conflict,
                    Auth.Registration.Errors.EmailAlreadyExists);
            }
            catch (UserCreationException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    Auth.Registration.Errors.CreationFailed);
            }
            catch (UserClaimException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    Auth.Registration.Errors.CreationFailed);
            }
            catch (AuthenticationException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.Unauthorized,
                    Auth.LoginM.Errors.InvalidCredentials);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.Unauthorized,
                    Auth.LoginM.Errors.UnauthorizedAccess);
            }
            catch (TimeoutException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.RequestTimeout,
                    Global.System.Timeout);
            }
            catch (SqlException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    Global.System.DatabaseError);
            }
            catch (DbUpdateException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    Global.System.DbUpdateError);
            }
            catch (OperationCanceledException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.RequestTimeout,
                    Global.System.RequestCancelled);
            }
            catch (ArgumentException ex)
            {
                await HandleExceptionAsync(context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    Auth.Token.Errors.GenerationFailed);
            }
            catch (Exception ex)
            {
                await HandleCriticalDatabaseExceptionAsync(context,
                ex, Global.System.DatabaseCriticalError);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context,
            Exception ex,
            HttpStatusCode httpStatusCode,
            string message)
        {
            await WriteResponseAsync(context, httpStatusCode, message);
        }

        private async Task HandleCriticalDatabaseExceptionAsync(HttpContext context, Exception ex, string userFriendlyMessage)
        {
            Log.Error(ex, LogMessages.System.DatabaseCriticalError, ex.Message);

            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, userFriendlyMessage);
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
