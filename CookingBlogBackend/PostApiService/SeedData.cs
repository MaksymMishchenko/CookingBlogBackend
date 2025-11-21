using Bogus;
using PostApiService.Models;

namespace PostApiService
{
    public static class SeedData
    {
        public static List<Post> GetPostsWithComments(int count = 10,
            bool useNewSeed = false,
            bool generateComments = true,
            int commentCount = 1,
            bool generateIds = false)
        {
            var posts = GetPostFaker(useNewSeed, generateComments, commentCount, generateIds).Generate(count);

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
            bool generateIds = false)
        {
            var seed = 0;
            if (useNewSeed)
            {
                seed = Random.Shared.Next(10, int.MaxValue);
            }

            return new Faker<Post>()
                .RuleFor(p => p.Id, _ => 0)
                .RuleFor(p => p.Title, f => f.Lorem.Sentence(3))
                .RuleFor(p => p.Description, f => f.Lorem.Paragraph(1))
                .RuleFor(p => p.Content, f => f.Lorem.Paragraphs(3))
                .RuleFor(p => p.Author, f => f.Person.FullName)
                .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
                .RuleFor(p => p.MetaTitle, f => f.Lorem.Sentence(2))
                .RuleFor(p => p.MetaDescription, f => f.Lorem.Sentence(8))
                .RuleFor(p => p.Slug, f => f.Lorem.Slug())
                .RuleFor(p => p.Comments, (f, post) =>
                {
                    if (!generateComments)
                        return new List<Comment>();

                    return new Faker<Comment>()
                        .RuleFor(c => c.Id, _ => 0)
                        .RuleFor(c => c.PostId, _ => post.Id)
                        .RuleFor(c => c.Author, fc => fc.Person.FullName)
                        .RuleFor(c => c.Content, fc => fc.Lorem.Sentence(3))
                        .RuleFor(c => c.UserId, _ => "testUserId")
                        .Generate(commentCount);
                })
                .UseSeed(seed);
        }
    }
}
