namespace PostApiService.Infrastructure.Common
{
    public class Result<T>
    {
        public T? Value { get; }
        public ResultStatus Status { get; }
        public string? ErrorMessage { get; }
        public bool IsSuccess => Status == ResultStatus.Success || Status == ResultStatus.NoContent;

        private Result(T value, ResultStatus status, string errorMessage = default!)
        {
            Value = value;
            Status = status;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Success(T value) => new(value, ResultStatus.Success);
        public static Result<T> NoContent() => new(default!, ResultStatus.NoContent);
        public static Result<T> NotFound(string message) => new(default!, ResultStatus.NotFound, message);
        public static Result<T> Conflict(string message) => new(default!, ResultStatus.Conflict, message);
        public static Result<T> Invalid(string message) => new(default!, ResultStatus.Invalid, message);
    }
}
