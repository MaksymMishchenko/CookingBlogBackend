using System.Net;

namespace PostApiService.Infrastructure.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? ErrorMessage { get; }
        public HttpStatusCode StatusCode { get; }

        private Result(bool isSuccess,
            T? value,
            string? errorMessage,
            HttpStatusCode statusCode)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            StatusCode = statusCode;
        }

        public static Result<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
            => new(true, value, null, statusCode);

        public static Result<T> NoContent(HttpStatusCode statusCode = HttpStatusCode.NoContent)
           => new(true, default, null, statusCode);

        public static Result<T> Failure(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            => new(false, default, errorMessage, statusCode);

        public static Result<T> Conflict(string message)
            => new(false, default, message, HttpStatusCode.Conflict);

        public static Result<T> NotFound(string message)
            => new(false, default, message, HttpStatusCode.NotFound);
    }
}
