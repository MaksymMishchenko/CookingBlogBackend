namespace PostApiService.Models
{
    public class PostResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public static PostResponse CreateErrorResponse(string message, List<string> errors = null)
        {
            return new PostResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
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
    }
}
