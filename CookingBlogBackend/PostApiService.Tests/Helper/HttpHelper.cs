namespace PostApiService.Tests.Helper
{
    public class HttpHelper
    {
        public static class Urls
        {                                  
            public static class Comments
            {                
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
