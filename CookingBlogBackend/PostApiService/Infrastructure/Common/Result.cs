namespace PostApiService.Infrastructure.Common
{
    public class Result<T>
    {
        public T? Value { get; }
        public ResultStatus Status { get; }
        public string? Message { get; }       
        public string? ErrorCode { get; }
        public bool IsSuccess => Status == ResultStatus.Success || Status == ResultStatus.NoContent;

        private Result(T value, ResultStatus status, string? message = null, string? errorCode = null)
        {
            Value = value;
            Status = status;
            Message = message;            
            ErrorCode = errorCode;
        }

        public static Result<T> Success(T value, string? message = null) => new(value, ResultStatus.Success, message);
        public static Result<T> NoContent() => new(default!, ResultStatus.NoContent);
        public static Result<T> NotFound(string message, string? errorCode = null) =>
            new(default!, ResultStatus.NotFound, message, errorCode: errorCode);
        public static Result<T> Conflict(string message, string? errorCode = null) =>
            new(default!, ResultStatus.Conflict, message, errorCode: errorCode);
        public static Result<T> Invalid(string message) => new(default!, ResultStatus.Invalid, message);
    }
}
