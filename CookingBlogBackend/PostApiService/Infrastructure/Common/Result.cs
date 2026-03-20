namespace PostApiService.Infrastructure.Common
{
    public class Result
    {
        public ResultStatus Status { get; protected set; }
        public string? Message { get; protected set; }
        public string? ErrorCode { get; protected set; }
        public IDictionary<string, string[]>? Errors { get; protected set; }
        public bool IsSuccess => Status == ResultStatus.Success ||
                                 Status == ResultStatus.NoContent ||
                                 Status == ResultStatus.Created;

        protected Result(ResultStatus status, string? message, string? errorCode, IDictionary<string, string[]>? errors = null)
        {
            Status = status;
            Message = message;
            ErrorCode = errorCode;
            Errors = errors;
        }
        public static Result Success(string? msg = null) => new(ResultStatus.Success, msg, null);
        public static Result Created(string? msg = null) => new(ResultStatus.Created, msg, null);
        public static Result NoContent() => new(ResultStatus.NoContent, null, null);
        public static Result NotFound(string msg, string? code = null) => new(ResultStatus.NotFound, msg, code);
        public static Result Conflict(string msg, string? code = null) => new(ResultStatus.Conflict, msg, code);
        public static Result Invalid(string msg, IDictionary<string, string[]>? errors = null, string? code = null)
            => new(ResultStatus.Invalid, msg, code, errors);
        public static Result Invalid(string msg, string? code = null) => new(ResultStatus.Invalid, msg, code);

        public static Result Error(string msg, string? code = null) => new(ResultStatus.Error, msg, code);
        public static Result Unauthorized(string msg, string? code = null) => new(ResultStatus.Unauthorized, msg, code);
        public static Result Forbidden(string msg, string? code = null) => new(ResultStatus.Forbidden, msg, code);
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        protected Result(T? value, ResultStatus status, string? message = null, string? errorCode = null, IDictionary<string, string[]>? errors = null)
            : base(status, message, errorCode, errors)
        {
            Value = value;
        }

        public static Result<T> Success(T value, string? message = null) =>
            new(value, ResultStatus.Success, message);

        public static Result<T> Created(T value, string? message = null) =>
            new(value, ResultStatus.Created, message);

        public new static Result<T> NotFound(string msg, string? code = null) => new(default, ResultStatus.NotFound, msg, code);
        public new static Result<T> Conflict(string msg, string? code = null) => new(default, ResultStatus.Conflict, msg, code);
        public new static Result<T> Invalid(string msg, IDictionary<string, string[]>? errors = null, string? code = null)
        => new(default, ResultStatus.Invalid, msg, code, errors);

        public new static Result<T> Invalid(string msg, string? code = null)
            => new(default, ResultStatus.Invalid, msg, code);
        public new static Result<T> Unauthorized(string msg, string? code = null) => new(default, ResultStatus.Unauthorized, msg, code);
        public new static Result<T> Forbidden(string msg, string? code = null) => new(default, ResultStatus.Forbidden, msg, code);
        public new static Result<T> Error(string msg, string? code = null) => new(default, ResultStatus.Error, msg, code);
    }
}
