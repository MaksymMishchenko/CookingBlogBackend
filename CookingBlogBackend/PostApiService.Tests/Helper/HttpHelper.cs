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
            public readonly static string GetAllPosts = "/api/Posts/GetAllPosts";
            public readonly static string PaginatedPostsUrl = "/api/Posts/GetAllPosts?pageNumber={0}&pageSize={1}";
            public readonly static string GetPostById = "/api/Posts/GetPost/{0}";
            public readonly static string AddPost = "/api/Posts/AddNewPost";
            public readonly static string UpdatePost = "/api/Posts/UpdatePost";
            public readonly static string DeletePost = "/api/Posts/DeletePost/{0}";

            public readonly static string AddComment = "/api/Comments/posts/1";
            public readonly static string UpdateComment = "api/Comments/2";
            public readonly static string DeleteComment = "api/Comments/3";
        }
    }
}
