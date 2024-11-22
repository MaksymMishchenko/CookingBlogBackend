using System.Text;
using System.Text.Json;

namespace PostApiService.Tests.Helper
{
    internal class HttpHelper
    {
        internal static StringContent GetJsonHttpContent(object item)
        {
            return new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
            //return new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");
        }

        internal static class Urls
        {
            public readonly static string GetAllPosts = "/api/Posts/GetAllPosts";
            public readonly static string GetPostById = "/api/Posts/GetAllById";
            public readonly static string AddPost = "/api/Posts/AddPostAsync";
            public readonly static string EditPost = "/api/Posts/EditPostAsync";
            public readonly static string DeletePost = "/api/Posts/DeletePostAsync";
        }
    }
}
