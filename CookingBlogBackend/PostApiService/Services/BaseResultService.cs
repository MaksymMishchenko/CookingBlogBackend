namespace PostApiService.Services
{
    public abstract class BaseResultService
    {
        protected Result NotFound(string message, string? code = null) => Result.NotFound(message, code);

        protected Result<T> NotFound<T>(string message, string code) => Result<T>.NotFound(message, code);

        protected Result Unauthorized()
        {
            return Result.Unauthorized(Auth.LoginM.Errors.UnauthorizedAccess,
                Auth.LoginM.Errors.UnauthorizedAccessCode);
        }

        protected Result<T> Unauthorized<T>(string? message = null, string? errorCode = null)
        {
            return Result<T>.Unauthorized(
                message ?? Auth.LoginM.Errors.UnauthorizedAccess,
                errorCode ?? Auth.LoginM.Errors.UnauthorizedAccessCode);
        }

        protected Result Forbidden(string message, string code) => Result.Forbidden(message, code);

        protected Result<T> Forbidden<T>(string message, string code) => Result<T>.Forbidden(message, code);

        protected Result<T> Invalid<T>(string message, string code) => Result<T>.Invalid(message, code);

        protected Result Conflict(string message, string code) => Result.Conflict(message, code);

        protected Result<T> Conflict<T>(string message, string code) => Result<T>.Conflict(message, code);

        protected Result<T> Error<T>(string message, string errorCode) => Result<T>.Error(message, errorCode);

        protected Result Success(string message) => Result.Success(message);

        protected Result<T> Success<T>(T data, string? message = null) => Result<T>.Success(data, message);
    }
}
