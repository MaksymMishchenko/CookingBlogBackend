using System.Text.Json.Serialization;

namespace PostApiService.Models.Common
{
    public class ApiResponse
    {
        public bool Success { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? EntityId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageNumber { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageSize { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TotalCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string SearchQuery { get; set; } = default!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Token { get; set; } = default!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]>? Errors { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        public static ApiResponse CreateErrorResponse
            (string message, IDictionary<string, string[]>? errors = null, string? errorCode = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors,
                ErrorCode = errorCode                
            };
        }
    }
    public class ApiResponse<T> : ApiResponse
    {

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T Data { get; set; } = default!;

        public static ApiResponse<T> CreateSuccessResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse<T> CreateSuccessResponse(string message, T? data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<IEnumerable<TData>> CreatePaginatedResponse<TData>(
            IEnumerable<TData> data,
            int pageNumber,
            int pageSize,
            int totalCount,
            string? message = null,
            string? searchQuery = null)
        {
            return new ApiResponse<IEnumerable<TData>>
            {
                Success = true,
                Message = message,
                SearchQuery = searchQuery,
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public static ApiResponse<T> CreateSuccessResponse(string message, int entityId)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                EntityId = entityId
            };
        }

        public static ApiResponse<T> CreateSuccessResponse(string message, string token)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Token = !string.IsNullOrEmpty(token) ? token : null
            };
        }
    }
}

