using Bogus;
using PostApiService.Models;

namespace PostApiService.Tests.Fixtures
{
    internal class DataFixture
    {
        private static int _commentIdCounter = 1;
        public static List<Post> GetPosts(int count, bool useNewSeed = false)
        {
            return GetPostFaker(useNewSeed).Generate(count);
        }

        public static Post GetPost(bool useNewSeed = false)
        {
            return GetPosts(1, useNewSeed)[0];
        }

        private static Faker<Post> GetPostFaker(bool useNewSeed)
        {
            var seed = 0;
            if (useNewSeed)
            {
                seed = Random.Shared.Next(10, int.MaxValue);
            }
            return new Faker<Post>()
               .RuleFor(p => p.PostId, f => 0)
            .RuleFor(p => p.Title, f => f.Lorem.Sentence(3))
            .RuleFor(p => p.Description, f => f.Lorem.Paragraph(1))
            .RuleFor(p => p.Content, f => f.Lorem.Paragraphs(3))
            .RuleFor(p => p.Author, f => f.Person.FullName)
            .RuleFor(p => p.CreateAt, f => f.Date.Past(1))
            .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(p => p.MetaTitle, f => f.Lorem.Sentence(2))
            .RuleFor(p => p.MetaDescription, f => f.Lorem.Paragraph(1))
            .RuleFor(p => p.Slug, f => f.Lorem.Slug())
            .RuleFor(p => p.Comments, f => new Faker<Comment>()
                .RuleFor(c => c.CommentId, fc => 0)
                .RuleFor(c => c.Author, fc => fc.Person.FullName)
                .RuleFor(c => c.Content, fc => fc.Lorem.Sentence(3))
                .RuleFor(c => c.CreatedAt, fc => fc.Date.Past(1))
                .Generate(f.Random.Int(0, 5)))
                .UseSeed(seed);
        }
    }
}
