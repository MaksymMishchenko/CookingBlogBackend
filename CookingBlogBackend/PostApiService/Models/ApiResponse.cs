﻿using System.Text.Json.Serialization;

namespace PostApiService.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<T>? DataList { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int EntityId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string[]>? Errors { get; set; }

        public static ApiResponse<T> CreateErrorResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message
            };
        }

        public static ApiResponse<T> CreateErrorResponse(string message, Dictionary<string, string[]>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new Dictionary<string, string[]>()
            };
        }

        public static ApiResponse<T> CreateSuccessResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse<T> CreateSuccessResponse(string message, T? data = default)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> CreateSuccessResponse(string message, List<T>? dataList = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                DataList = dataList ?? new List<T>()
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
