namespace PostApiService.Services
{
    public abstract class BaseResultService
    {
        // --- Success ---
        protected Result Success(string? message = null) => Result.Success(message);
        protected Result<T> Success<T>(T data, string? message = null) => Result<T>.Success(data, message);

        // --- NotFound ---
        protected Result NotFound(string message, string? code = null) => Result.NotFound(message, code);
        protected Result<T> NotFound<T>(string message, string? code = null) => Result<T>.NotFound(message, code);

        // --- Unauthorized ---
        protected Result Unauthorized(string? message = null, string? code = null) =>
            Result.Unauthorized(message ?? Auth.LoginM.Errors.UnauthorizedAccess, code ?? Auth.LoginM.Errors.UnauthorizedAccessCode);

        protected Result<T> Unauthorized<T>(string? message = null, string? code = null) =>
            Result<T>.Unauthorized(message ?? Auth.LoginM.Errors.UnauthorizedAccess, code ?? Auth.LoginM.Errors.UnauthorizedAccessCode);

        // --- Forbidden ---
        protected Result Forbidden(string? message = null, string? code = null) =>
            Result.Forbidden(message ?? Auth.LoginM.Errors.AccessForbidden, code ?? Auth.LoginM.Errors.AccessForbiddenErrorCode);

        protected Result<T> Forbidden<T>(string? message = null, string? code = null) =>
            Result<T>.Forbidden(message ?? Auth.LoginM.Errors.AccessForbidden, code ?? Auth.LoginM.Errors.AccessForbiddenErrorCode);

        // --- Invalid ---
        protected Result<T> Invalid<T>(string message, string? code = null) => Result<T>.Invalid(message, code);
        protected Result<T> Invalid<T>(string message, IDictionary<string, string[]> errors, string? code = null) =>
            Result<T>.Invalid(message, errors, code);

        // --- Conflict ---
        protected Result Conflict(string message, string? code = null) => Result.Conflict(message, code);
        protected Result<T> Conflict<T>(string message, string? code = null) => Result<T>.Conflict(message, code);

        // --- Error ---
        protected Result<T> Error<T>(string message, string errorCode) => Result<T>.Error(message, errorCode);
    }
}