using PostApiService.Models;

namespace PostApiService.Tests.Helper
{
    internal class TestDataHelper
    {
        public static List<Post> GetPostsWithComments(int count,
            bool useNewSeed = false,
            bool generateComments = true,
            int commentCount = 1)
        {
            return GetPostFaker(useNewSeed, generateComments, commentCount).Generate(count);
        }

        public static Post GetPostWithComments(bool useNewSeed = false,
            bool generateComments = true,
            int commentCount = 1)
        {
            return GetPostsWithComments(1, useNewSeed, generateComments, commentCount)[0];
        }

        private static Faker<Post> GetPostFaker(bool useNewSeed,
            bool generateComments,
            int commentCount)
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
                    if (generateComments)
                    {
                        return new Faker<Comment>()
                            .RuleFor(c => c.CommentId, _ => 0)
                            .RuleFor(c => c.Author, fc => fc.Person.FullName)
                            .RuleFor(c => c.Content, fc => fc.Lorem.Sentence(3))
                            .RuleFor(c => c.PostId, _ => post.PostId)
                            .RuleFor(c => c.UserId, _ => "testUserId")
                            .Generate(commentCount);
                    }
                    return new List<Comment>();
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
                ImageUrl = "http://example.com/img/img.jpg",
                Slug = "post-slug",
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
                new Comment { PostId = 1, CommentId = 1, UserId = "testUserId", Content = "This is the test comment 1." },
                new Comment { PostId = 1, CommentId = 2, UserId = "testUserId", Content = "This is the test comment 2." },
                new Comment { PostId = 1, CommentId = 3, UserId = "testUserId", Content = "This is the test comment 3." }
            };
        }

        public static List<Comment> GetEmptyCommentList()
        {
            return new List<Comment>();
        }

        public static List<Post> GetPostsWithComments()
        {
            return new List<Post> {
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 1",
                    Description = "Description lorem ipsum dolor sit amet 1",
                    Author = "Lorem 1",
                    Content = "Simple comtemt lorem ipsum dolor sit amet 1",
                    ImageUrl = "http://img-1.com",
                    MetaTitle = "Meta title dolor sit amet 1",
                    MetaDescription = "Meta lorem ipsum dolor 1",
                    Slug = "post-slug-1",
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            Author = "Comment author 1",
                            PostId = 1,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            Author = "Comment author 2",
                            PostId = 1,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            Author = "Comment author 3",
                            PostId = 1,
                            UserId = "testUserId"
                        }
                    }
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 2",
                    Description = "Description lorem ipsum dolor sit amet 2",
                    Author = "Lorem 2",
                    Content = "Simple comtemt lorem ipsum dolor sit amet 2",
                    ImageUrl = "http://img-2.com",
                    MetaTitle = "Meta title dolor sit amet 2",
                    MetaDescription = "Meta lorem ipsum dolor 2",
                    Slug = "post-slug-2",
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            Author = "Comment author 1",
                            PostId = 2,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            Author = "Comment author 2",
                            PostId = 2,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            Author = "Comment author 3",
                            PostId = 2,
                            UserId = "testUserId"
                        }
                    }
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 3",
                    Description = "Description lorem ipsum dolor sit amet 3",
                    Author = "Lorem 3",
                    Content = "Simple comtemt lorem ipsum dolor sit amet 3",
                    ImageUrl = "http://img-3.com",
                    MetaTitle = "Meta title dolor sit amet 3",
                    MetaDescription = "Meta lorem ipsum dolor 3",
                    Slug = "post-slug-3",
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            Author = "Comment author 1",
                            PostId = 3,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            Author = "Comment author 2",
                            PostId = 3,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            Author = "Comment author 3",
                            PostId = 3,
                            UserId = "testUserId"
                        }
                    }
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 4",
                    Description = "Description lorem ipsum dolor sit amet 4",
                    Author = "Lorem 4",
                    Content = "Simple comtemt lorem ipsum dolor sit amet 4",
                    ImageUrl = "http://img-4.com",
                    MetaTitle = "Meta title dolor sit amet 4",
                    MetaDescription = "Meta lorem ipsum dolor 4",
                    Slug = "post-slug-4",
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            Author = "Comment author 1",
                            PostId = 4,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            Author = "Comment author 2",
                            PostId = 4,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            Author = "Comment author 3",
                            PostId = 4,
                            UserId = "testUserId"
                        }
                    }
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 5",
                    Description = "Description lorem ipsum dolor sit amet 5",
                    Author = "Lorem 5",
                    Content = "Simple comtemt lorem ipsum dolor sit amet 5",
                    ImageUrl = "http://img-5.com",
                    MetaTitle = "Meta title dolor sit amet 5",
                    MetaDescription = "Meta lorem ipsum dolor 5",
                    Slug = "post-slug-5",
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            Author = "Comment author 1",
                            PostId = 5,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            Author = "Comment author 2",
                            PostId = 5,
                            UserId = "testUserId"
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            Author = "Comment author 3",
                            PostId = 5,
                            UserId = "testUserId"
                        }
                    }
                }
            };
        }
    }
}
