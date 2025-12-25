using System.Text.Json.Serialization;

namespace PostApiService.Models
{
    public class ApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? EntityId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageSize { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageNumber { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TotalCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string SearchQuery { get; set; } = default!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Token { get; set; } = default!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]>? Errors { get; set; }

        public static ApiResponse CreateErrorResponse(string message, IDictionary<string, string[]>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }
    public class ApiResponse<T> : ApiResponse
    {

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T Data { get; set; } = default!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<T> DataList { get; set; } = default!;

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

        public static ApiResponse<T> CreateSingleItemResponse(string message, T? data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> CreatePaginatedListResponse(
            string message,
            List<T>? dataList = null,
            int? pageNumber = 1,
            int? pageSize = 10,
            int? totalCount = 0)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                DataList = dataList ?? new List<T>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public static ApiResponse<T> CreatePaginatedSearchListResponse(
            string message,
            string searchQuery,
            List<T>? dataList = null,
            int? pageNumber = 1,
            int? pageSize = 10,
            int? totalSearchCount = 0)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                SearchQuery = searchQuery,
                DataList = dataList ?? new List<T>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalSearchCount
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

