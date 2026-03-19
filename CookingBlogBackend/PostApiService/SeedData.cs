using Bogus;
using PostApiService.Helper;

namespace PostApiService
{
    public static class SeedData
    {
        public static List<Post> GetPostsWithComments(int count = 10,
            bool useNewSeed = false,
            bool generateComments = true,
            int commentCount = 1,
            string[] userIds = null!,
            bool generateIds = false)
        {
            var posts = GetPostFaker(useNewSeed, generateComments, commentCount, userIds, generateIds).Generate(count);

            if (generateIds)
            {
                int postId = 1;
                int commentId = 1;

                foreach (var post in posts)
                {
                    post.Id = postId++;

                    if (post.Comments != null)
                    {
                        foreach (var comment in post.Comments)
                        {
                            comment.Id = commentId++;
                            comment.PostId = post.Id;
                        }
                    }
                }
            }

            return posts;
        }

        private static Faker<Post> GetPostFaker(bool useNewSeed,
            bool generateComments,
            int commentCount,
            string[] userIds = null!,
            bool generateIds = false)
        {
            var culinaryCategories = new[]
            {
                new Category { Name = "Breakfast", Slug = StringHelper.GenerateSlug("Breakfast") },
                new Category { Name = "Main Course", Slug = StringHelper.GenerateSlug("Main Course") },
                new Category { Name = "Desserts", Slug = StringHelper.GenerateSlug("Desserts") },
                new Category { Name = "Healthy Food", Slug = StringHelper.GenerateSlug("Healthy Food") },
                new Category { Name = "Beverages", Slug = StringHelper.GenerateSlug("Beverages") },
                new Category { Name = "Vegetarian", Slug = StringHelper.GenerateSlug("Vegetarian") }
            };

            var seed = 0;
            if (useNewSeed)
            {
                seed = Random.Shared.Next(10, int.MaxValue);
            }

            return new Faker<Post>()
                .RuleFor(p => p.Id, _ => 0)
                .RuleFor(p => p.Title, f => f.Lorem.Sentence(3))
                .RuleFor(p => p.Description, f =>
                {
                    var text = f.Lorem.Sentences(f.Random.Int(5, 10), " ");
                    return text.Length > 1000 ? text.Substring(0, 1000) : text;
                })
                .RuleFor(p => p.Content, f => f.Lorem.Paragraphs(3))
                .RuleFor(p => p.Category, f => f.PickRandom(culinaryCategories))
                .RuleFor(p => p.Author, f => f.Person.FullName)
                .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
                .RuleFor(p => p.MetaTitle, f => f.Lorem.Sentence(2))
                .RuleFor(p => p.MetaDescription, f => f.Lorem.Sentence(8))
                .RuleFor(p => p.Slug, f => f.Lorem.Slug())
                .RuleFor(p => p.UpdatedAt, f => f.Date.Recent(7).ToUniversalTime())
                .RuleFor(p => p.Comments, (f, post) =>
                {
                    if (!generateComments)
                        return new List<Comment>();

                    var roots = new Faker<Comment>()
                        .RuleFor(c => c.Content, fc => fc.Lorem.Sentence())
                        .RuleFor(c => c.UserId, fc => f.PickRandom(userIds))
                        .RuleFor(c => c.CreatedAt, fc => fc.Date.Recent(5).ToUniversalTime())
                        .Generate(commentCount);

                    var allComments = new List<Comment>(roots);

                    foreach (var root in roots)
                    {
                        int replyCount = f.Random.Int(0, 3);
                        if (replyCount > 0)
                        {
                            var replies = new Faker<Comment>()
                                .RuleFor(c => c.Content, fc => fc.Lorem.Sentence())
                                .RuleFor(c => c.UserId, fc => f.PickRandom(userIds))
                                .RuleFor(c => c.CreatedAt, fc => fc.Date.Between(root.CreatedAt, DateTime.UtcNow).ToUniversalTime())
                                .RuleFor(c => c.Parent, _ => root)
                                .RuleFor(c => c.ReplyToUserName, _ => "User_" + f.PickRandom(userIds).Substring(0, 4))
                                .Generate(replyCount);

                            allComments.AddRange(replies);
                        }
                    }
                    return allComments;
                })
                .UseSeed(seed);
        }
    }
}