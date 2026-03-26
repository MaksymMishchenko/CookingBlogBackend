namespace PostApiService.Tests.Helper
{
    public static class PostTestExtensions
    {
        public static List<Post> WithCommentHierarchy(this List<Post> posts, int commentCount = 3, string? userId = null)
        {
            userId ??= "test-user-id";

            foreach (var post in posts)
            {
                var root = new Comment
                {
                    Id = 0,
                    Content = "Parent Comment",
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Post = post,
                    Replies = new List<Comment>()
                };

                for (int i = 1; i < commentCount; i++)
                {
                    var reply = new Comment
                    {
                        Id = 0,
                        Content = $"Reply {i}",
                        UserId = userId,
                        CreatedAt = root.CreatedAt.AddMinutes(i),
                        Post = post
                    };

                    root.Replies.Add(reply);
                }

                post.Comments ??= new List<Comment>();
                post.Comments.Add(root);
            }
            return posts;
        }
    }
}
