namespace PostApiService.Models.TypeSafe
{
    public class TS
    {
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Contributor = "Contributor";
        }

        public static class Controller
        {
            public const string Post = "Post";
            public const string Comment = "Comment";
        }

        public static class Permissions
        {
            public const int None = 0;
            public const int Read = 1;
            public const int Write = 2;
            public const int Update = 3;
            public const int Delete = 4;
        }
        public static class Policies
        {
            public const string FullControlPolicy = "FullControlPolicy";
        }
    }
}
