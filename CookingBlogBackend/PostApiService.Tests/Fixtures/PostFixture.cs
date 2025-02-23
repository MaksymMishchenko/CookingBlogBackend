namespace PostApiService.Tests.Fixtures
{
    public class PostFixture : TestBaseFixture
    {
        private const string _connectionString = "Server=MAX\\SQLEXPRESS;Database=TestPost;Trusted_Connection=True;" +
            "MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public PostFixture() : base(_connectionString, useDatabase: true) { }
    }
}
