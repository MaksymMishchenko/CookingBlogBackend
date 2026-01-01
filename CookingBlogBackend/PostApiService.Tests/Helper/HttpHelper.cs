using System.Text;

namespace PostApiService.Tests.Helper
{
    public class HttpHelper
    {
        public static StringContent GetJsonHttpContent(object item)
        {
            return new StringContent(System.Text.Json.JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
        }

        public static class Urls
        {
            public static class Categories
            {
                public const string Base = "/api/category";
                public const string GetById = "/api/category/{0}";                               
            }

            public static class Posts
            {
                public const string Base = "/api/posts";
                public const string GetById = "/api/posts/{0}";
                public const string Search = "/api/posts/search?queryString={0}&pageNumber={1}&pageSize={2}";
                public const string Paginated = "/api/posts?pageNumber={0}&pageSize={1}";
            }

            public static class Comments
            {
                public const string Base = "/api/comments";
                public const string GetById = "/api/comments/{0}";
            }

            public static class Authentication
            {
                public const string Login = "/api/auth/login";
                public const string Register = "/api/auth/register";
            }
        }
    }
}
