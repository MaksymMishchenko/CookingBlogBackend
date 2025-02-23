namespace PostApiService.Tests.Fixtures
{
    public class CommentFixture : TestBaseFixture
    {
        private const string _connectionString = "Server=MAX\\SQLEXPRESS;Database=TestComment;Trusted_Connection=True;" +
            "MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public CommentFixture() : base(_connectionString, useDatabase: true) { }
    }
}
