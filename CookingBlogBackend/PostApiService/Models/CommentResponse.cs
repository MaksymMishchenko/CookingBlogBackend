namespace PostApiService.Models
{
    public class CommentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public static CommentResponse CreateErrorResponse(string message, List<string> errors = null)
        {
            return new CommentResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }

        public static CommentResponse CreateSuccessResponse(string message)
        {
            return new CommentResponse
            {
                Success = true,
                Message = message
            };
        }
    }
}
