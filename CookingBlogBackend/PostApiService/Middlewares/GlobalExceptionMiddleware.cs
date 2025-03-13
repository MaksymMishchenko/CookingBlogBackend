using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Models;
using System.Net;
using System.Security.Authentication;

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
            catch (UserAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Conflict,
                    RegisterErrorMessages.UsernameAlreadyExists);
            }
            catch (EmailAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Conflict,
                    RegisterErrorMessages.EmailAlreadyExists);
            }
            catch (UserCreationException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    RegisterErrorMessages.CreationFailed);
            }
            catch (UserClaimException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    RegisterErrorMessages.CreationFailed);
            }
            catch (AuthenticationException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Unauthorized,
                    AuthErrorMessages.InvalidCredentials);
            }
            catch (PostNotFoundException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.NotFound,
                    string.Format(PostErrorMessages.PostNotFound, ex.PostId));
            }
            catch (PostAlreadyExistException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Conflict,
                    string.Format(PostErrorMessages.PostAlreadyExist, ex.Title));
            }
            catch (AddPostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(PostErrorMessages.AddPostFailed, ex.Title));
            }
            catch (UpdatePostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(PostErrorMessages.UpdatePostFailed, ex.Title));
            }
            catch (DeletePostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(PostErrorMessages.DeletePostFailed, ex.PostId));
            }
            catch (CommentNotFoundException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.NotFound,
                    string.Format(CommentErrorMessages.CommentNotFound, ex.CommentId));
            }
            catch (AddCommentFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(CommentErrorMessages.AddCommentFailed, ex.PostId));
            }
            catch (UpdateCommentFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(CommentErrorMessages.UpdateCommentFailed, ex.CommentId));
            }
            catch (DeleteCommentFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(CommentErrorMessages.DeleteCommentFailed, ex.CommentId));
            }            
            catch (TimeoutException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.RequestTimeout,
                    ResponseErrorMessages.TimeoutException);
            }
            catch (SqlException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ResponseErrorMessages.SqlException);
            }            
            catch (DbUpdateException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ResponseErrorMessages.DbUpdateException);
            }
            catch (OperationCanceledException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.RequestTimeout,
                    ResponseErrorMessages.OperationCanceledException);
            }
            catch (ArgumentException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    TokenErrorMessages.GenerationFailed);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    ResponseErrorMessages.UnexpectedErrorException);
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

            var errorResponse = ApiResponse<object>.CreateErrorResponse(message);

            await response.WriteAsJsonAsync(errorResponse);
        }
    }
}
