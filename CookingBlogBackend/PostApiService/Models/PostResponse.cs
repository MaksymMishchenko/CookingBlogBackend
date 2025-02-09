using System.Text.Json.Serialization;

namespace PostApiService.Models
{
    public class PostResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }        
        public int PostId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string[]>? Errors { get; set; }

        public static PostResponse CreateErrorResponse(string message, Dictionary<string, string[]>? errors = null)
        {
            return new PostResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new Dictionary<string, string[]>()
            };
        }

        public static PostResponse CreateSuccessResponse(string message)
        {
            return new PostResponse
            {
                Success = true,
                Message = message
            };
        }

        public static PostResponse CreateSuccessResponse(string message, int postId)
        {
            return new PostResponse
            {
                Success = true,
                Message = message,
                PostId = postId
            };
        }
    }
}
