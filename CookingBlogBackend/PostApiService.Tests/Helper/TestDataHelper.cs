using PostApiService.Models;
using PostApiService.Models.Dto;

namespace PostApiService.Tests.Helper
{
    internal class TestDataHelper
    {
        public static List<Post> GetPostsWithComments(int count,
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
            bool generateIds)
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
                .RuleFor(p => p.MetaDescription, f => f.Lorem.Paragraph(1))
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

        public static List<PostListDto> GetPostListDtos(int count)
        {
            var posts = GetPostsWithComments(count, generateIds: true);

            return posts.Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Author = p.Author,
                CreatedAt = p.CreateAt,
                Description = p.Description,
                CommentsCount = p.Comments?.Count ?? 0
            }).ToList();
        }

        public static List<PostListDto> GetEmptyPostListDtos()
        {
            return new List<PostListDto>();
        }

        public static void AssertPostListDtoMapping(Post expectedPost, PostListDto actualDto, int expectedCommentCount)
        {
            Assert.NotNull(actualDto);
            Assert.Equal(expectedPost.Id, actualDto.Id);
            Assert.Equal(expectedPost.Title, actualDto.Title);
            Assert.Equal(expectedPost.Slug, actualDto.Slug);
            Assert.Equal(expectedPost.Author, actualDto.Author);
            Assert.Equal(expectedPost.Description, actualDto.Description);
            Assert.Equal(expectedPost.CreateAt, actualDto.CreatedAt);
            Assert.Equal(expectedCommentCount, actualDto.CommentsCount);
        }

        public static Post GetSinglePost(int? id = 1, bool includeId = true)
        {
            int finalId = 0;

            if (includeId && id.HasValue)
            {
                finalId = id.Value;
            }

            return new Post
            {
                Id = finalId,
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

        public static List<Post> GetEmptyPostList()
        {
            return new List<Post>();
        }

        public static List<Comment> GetListWithComments()
        {
            return new List<Comment> {
                new Comment { PostId = 1, Id = 1, UserId = "testUserId", Content = "This is the test comment 1." },
                new Comment { PostId = 1, Id = 2, UserId = "testUserId", Content = "This is the test comment 2." },
                new Comment { PostId = 1, Id = 3, UserId = "testUserId", Content = "This is the test comment 3." }
            };
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
