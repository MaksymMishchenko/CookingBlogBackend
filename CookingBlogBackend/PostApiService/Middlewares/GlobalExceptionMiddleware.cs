using Microsoft.Data.SqlClient;
using PostApiService.Exceptions;
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
            catch (UserNotFoundException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Unauthorized,
                    Auth.LoginM.Errors.InvalidCredentials);
            }
            catch (UserAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Conflict,
                    Auth.Registration.Errors.UsernameAlreadyExists);
            }
            catch (EmailAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Conflict,
                    Auth.Registration.Errors.EmailAlreadyExists);
            }
            catch (UserCreationException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    Auth.Registration.Errors.CreationFailed);
            }
            catch (UserClaimException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    Auth.Registration.Errors.CreationFailed);
            }
            catch (AuthenticationException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Unauthorized,
                    Auth.LoginM.Errors.InvalidCredentials);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Unauthorized,
                    Auth.LoginM.Errors.UnauthorizedAccess);
            }
            catch (PostNotFoundException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.NotFound,
                    string.Format(PostM.Errors.PostNotFound, ex.PostId));
            }
            catch (PostAlreadyExistException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.Conflict,
                    string.Format(PostM.Errors.PostAlreadyExist, ex.Title));
            }
            catch (AddPostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(PostM.Errors.AddPostFailed, ex.Title));
            }
            catch (UpdatePostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(PostM.Errors.UpdatePostFailed, ex.Title));
            }
            catch (DeletePostFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(PostM.Errors.DeletePostFailed, ex.PostId));
            }
            catch (CommentNotFoundException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.NotFound,
                    string.Format(CommentM.Errors.CommentNotFound, ex.CommentId));
            }
            catch (AddCommentFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(CommentM.Errors.AddCommentFailed, ex.PostId));
            }
            catch (UpdateCommentFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(CommentM.Errors.UpdateCommentFailed, ex.CommentId));
            }
            catch (DeleteCommentFailedException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    string.Format(CommentM.Errors.DeleteCommentFailed, ex.CommentId));
            }
            catch (TimeoutException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.RequestTimeout,
                    Global.System.Timeout);
            }
            catch (SqlException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    Global.System.DatabaseError);
            }
            catch (DbUpdateException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    Global.System.DbUpdateError);
            }
            catch (OperationCanceledException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.RequestTimeout,
                    Global.System.RequestCancelled);
            }
            catch (ArgumentException ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    Auth.Token.Errors.GenerationFailed);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context,
                    ex.Message,
                    HttpStatusCode.InternalServerError,
                    Global.Validation.UnexpectedErrorException);
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

            var errorResponse = ApiResponse.CreateErrorResponse(message);

            await response.WriteAsJsonAsync(errorResponse);
        }
    }
}
