using System.Text;

namespace PostApiService.Tests.Helper
{
    internal class HttpHelper
    {
        public static StringContent GetJsonHttpContent(object item)
        {           
            return new StringContent(System.Text.Json.JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
        }

        internal static class Urls
        {            
            public readonly static string PaginatedPostsUrl = "/api/posts?pageNumber={0}&pageSize={1}";
            public readonly static string GetPostById = "/api/Posts/{0}";
            public readonly static string AddPost = "/api/posts";
            public readonly static string UpdatePost = "/api/posts/2";
            public readonly static string DeletePost = "/api/Posts/{0}";

            public readonly static string AddComment = "/api/comments/{0}";
            public readonly static string UpdateComment = "/api/comments/2";
            public readonly static string DeleteComment = "/api/comments/3";
        }
    }
}
