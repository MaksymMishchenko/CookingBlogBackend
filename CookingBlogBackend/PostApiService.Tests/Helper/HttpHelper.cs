namespace PostApiService.Tests.Helper
{
    public class HttpHelper
    {
        public static class Urls
        {
            public static class Categories
            {
                public const string Base = "/api/category";
                public const string AdminBase = "/api/admin/categories";
                public const string GetById = "/api/admin/categories/{0}";
            }

            public static class Posts
            {
                public const string Base = "/api/posts";
                public const string AdminBase = "/api/admin/posts";
                public const string GetById = "/api/admin/posts/{0}";
                public const string GetBySlug = "/api/posts/{0}/{1}";
                public const string GetByCategorySlug = "/api/posts/category/{0}?pageNumber={1}&pageSize={2}";
                public const string Search = "/api/posts/search?queryString={0}&pageNumber={1}&pageSize={2}";
                public const string Paginated = "/api/posts?pageNumber={0}&pageSize={1}";                
                public const string AdminPaginated = "/api/admin/posts?search={0}&categorySlug={1}&onlyActive={2}&pageNumber={3}&pageSize={4}";
                public const string GetComments = "/api/posts/{0}/comments?pageNumber={1}&pageSize={2}";
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
