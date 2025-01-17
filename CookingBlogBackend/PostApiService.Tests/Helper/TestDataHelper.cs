using Bogus;
using PostApiService.Models;

namespace PostApiService.Tests.Helper
{
    internal class TestDataHelper
    {
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
            .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(p => p.MetaTitle, f => f.Lorem.Sentence(2))
            .RuleFor(p => p.MetaDescription, f => f.Lorem.Paragraph(1))
            .RuleFor(p => p.Slug, f => f.Lorem.Slug())
            .RuleFor(p => p.Comments, (f, post) =>
            {
                return new Faker<Comment>()
                    .RuleFor(c => c.CommentId, _ => 0)
                    .RuleFor(c => c.Author, fc => fc.Person.FullName)
                    .RuleFor(c => c.Content, fc => fc.Lorem.Sentence(3))
                    .RuleFor(c => c.PostId, _ => post.PostId)
                    .Generate(f.Random.Int(0, 5));
            })
            .UseSeed(seed);
        }

        public static Post GetSinglePost()
        {
            return new Post
            {
                PostId = 1,
                Title = "Lorem ipsum dolor sit amet",
                Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry.",
                Author = "Test author",
                Description = "Test description",
                MetaTitle = "Test meta title",
                MetaDescription = "Test meta description",
                ImageUrl = "img/img.jpg",
                Slug = "post-slug"
            };
        }

        public static List<Post> GetListWithPost()
        {
            return new List<Post> { new Post { PostId = 1 } };
        }

        public static List<Post> GetEmptyPostList()
        {
            return new List<Post>();
        }

        public static List<Comment> GetListWithComments()
        {
            return new List<Comment> {
                new Comment { PostId = 1, CommentId = 1, Content = "This is the test comment 1." },
                new Comment { PostId = 1, CommentId = 2, Content = "This is the test comment 2." },
                new Comment { PostId = 1, CommentId = 3, Content = "This is the test comment 3." },

            };
        }

        public static List<Comment> GetEmptyCommentList()
        {
            return new List<Comment>();
        }
    }
}
