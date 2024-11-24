using System.Text;

namespace PostApiService.Tests.Helper
{
    internal class HttpHelper
    {
        public static StringContent GetJsonHttpContent(object item)
        {
            //return new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
            return new StringContent(System.Text.Json.JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
        }

        internal static class Urls
        {
            public readonly static string GetAllPosts = "/api/Posts/GetAllPosts";
            public readonly static string GetPostById = "/api/Posts/GetPost";
            public readonly static string AddPost = "/api/Posts/AddNewPost";
            public readonly static string EditPost = "/api/Posts/EditPostAsync";
            public readonly static string DeletePost = "/api/Posts/DeletePostAsync";
        }
    }
}
