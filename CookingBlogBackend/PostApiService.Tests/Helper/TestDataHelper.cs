using Bogus.Extensions;
using PostApiService.Helper;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.Helper
{
    public class TestDataHelper
    {
        public static List<Post> GetPostsWithComments(int count,
            ICollection<Category>? categories,
            bool useNewSeed = false,
            bool generateComments = true,
            int commentCount = 1,
            bool generateIds = false,
            Category? forcedCategory = null)
        {
            var posts = GetPostFaker(useNewSeed, categories!, generateComments, commentCount, generateIds, forcedCategory).Generate(count);

            if (generateIds)
            {
                int postId = 1;
                int commentId = 1;
                var baseTime = DateTime.UtcNow;

                foreach (var post in posts)
                {
                    post.Id = postId++;
                    post.CreatedAt = baseTime.AddMinutes(-post.Id);

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
            ICollection<Category> categories,
            bool generateComments,
            int commentCount,
            bool generateIds,
            Category? forcedCategory = null)
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
                .RuleFor(p => p.Category, f => forcedCategory ?? f.PickRandom(categories))
                .RuleFor(p => p.Author, f => f.Person.FullName)
                .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
                .RuleFor(p => p.MetaTitle, f => f.Lorem.Sentence(2))
                .RuleFor(p => p.MetaDescription, f => f.Lorem.Sentence(3).ClampLength(50, 200))
                .RuleFor(p => p.Slug, f => f.Lorem.Slug())
                .RuleFor(p => p.UpdatedAt, f => f.Date.Recent(7).ToUniversalTime())
                .RuleFor(p => p.Comments, (f, post) =>
                {
                    if (!generateComments || commentCount <= 0)
                        return new List<Comment>();

                    return new Faker<Comment>()
                        .RuleFor(c => c.Id, _ => 0)
                        .RuleFor(c => c.PostId, _ => post.Id)
                        .RuleFor(c => c.Content, fc => fc.Lorem.Sentence(3))
                        .RuleFor(c => c.UserId, _ => "testContId")
                        .Generate(commentCount);
                })
                .UseSeed(seed);
        }
       
        public static PostCreateDto ToPostCreateDto(Post post)
        {
            return new PostCreateDto
            {
                Title = post.Title,
                Description = post.Description,
                Content = post.Content,
                Author = post.Author,
                ImageUrl = post.ImageUrl,
                MetaTitle = post.MetaTitle,
                MetaDescription = post.MetaDescription,
                Slug = post.Slug,
                CategoryId = post.CategoryId
            };
        }
        public static PostUpdateDto ToPostUpdateDto(Post post, string? newTitle = null)
        {
            return new PostUpdateDto
            {
                Title = newTitle ?? post.Title,
                Description = post.Description,
                Content = post.Content,
                Author = post.Author,
                ImageUrl = post.ImageUrl,
                MetaTitle = post.MetaTitle,
                MetaDescription = post.MetaDescription,
                Slug = post.Slug,
                CategoryId = post.CategoryId
            };
        }

        public static void AssertCategoryAsync(Category expectedCategory, CategoryDto actualDto)
        {
            Assert.NotNull(actualDto);
            Assert.Equal(expectedCategory.Id, actualDto.Id);
            Assert.Equal(expectedCategory.Name, actualDto.Name);
        }

        public static Post GetSinglePost(ICollection<Category>? categories = null, int? id = 1, bool includeId = true)
        {
            int finalId = 0;

            if (includeId && id.HasValue)
            {
                finalId = id.Value;
            }

            var category = categories!.First(c => c.Name == "Desserts");

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
                CategoryId = category.Id,
                Category = null!
            };
        }

        public static Post GetCreatePostDto(string content, ICollection<Category>? categories = null)
        {
            var category = categories!.First(c => c.Name == "Desserts");

            return new Post
            {
                Title = "Lorem ipsum dolor sit amet",
                Content = content,
                Author = "Test author",
                Description = "Test description",
                MetaTitle = "Test meta title",
                MetaDescription = "Test meta description",
                ImageUrl = "http://example.com/img/img.jpg",
                Slug = "post-slug",
                CategoryId = category.Id,
                Category = null!
            };
        }

        public static Post GetSinglePostWithCategoryId(int categoryId)
        {
            return new Post
            {
                Title = "Valid Test Title",
                Slug = "valid-test-slug",
                Content = "Full content of the test post.",
                Description = "Brief description",
                Author = "Author Name",
                ImageUrl = "http://example.com/image.jpg",
                MetaTitle = "Meta Title",
                MetaDescription = "Meta Description",
                CategoryId = categoryId
            };
        }

        public static List<Post> GetPostsWithComments(ICollection<Category> categories, string userId = "testContId")
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
                    Category = categories.First(c => c.Name == "Beverages"),
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            PostId = 1,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            PostId = 1,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            PostId = 1,
                            UserId = userId
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
                    Category = categories.First(c => c.Name == "Vegetarian"),
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            PostId = 2,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            PostId = 2,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            PostId = 2,
                            UserId = userId
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
                    Category = categories.First(c => c.Name == "Desserts"),
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            PostId = 3,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            PostId = 3,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            PostId = 3,
                            UserId = userId
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
                    Category = categories.First(c => c.Name == "Breakfast"),
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            PostId = 4,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            PostId = 4,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            PostId = 4,
                            UserId = userId
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
                    Category = categories.First(c => c.Name == "Healthy Food"),
                    Comments = new List<Comment>{
                        new Comment{
                            Content = "Post comment content 1",
                            PostId = 5,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 2",
                            PostId = 5,
                            UserId = userId
                        },
                        new Comment{
                            Content = "Post comment content 3",
                            PostId = 5,
                            UserId = userId
                        }
                    }
                }
            };
        }

        public static List<Post> GetAdminTestPosts(ICollection<Category> categories)
        {
            var posts = GetPostsWithComments(categories);

            posts[0].IsActive = true;
            posts[1].IsActive = false;
            posts[2].IsActive = true;
            posts[3].IsActive = false;
            posts[4].IsActive = true;

            return posts;
        }

        public static IEnumerable<object[]> GetPostFilterData()
        {
            yield return new object[] { null!, null!, null!, 5, null! };
            yield return new object[] { null!, null!, true, 3, null! };
            yield return new object[] { null!, null!, false, 2, null! };

            yield return new object[] { "Lorem", null!, null!, 5, null! };
            yield return new object[] { "Lorem", null!, true, 3, null! };
            yield return new object[] { "1", null!, null!, 1, null! };


            yield return new object[] { null!, "beverages", null!, 1, "Beverages" };
            yield return new object[] { null!, "beverages", true, 1, "Beverages" };
            yield return new object[] { null!, "beverages", false, 0, "Beverages" };

            yield return new object[] { "Lorem", "desserts", true, 1, "Desserts" };
            yield return new object[] { "Lorem", "vegetarian", false, 1, "Vegetarian" };
        }

        public static List<Category> GetCulinaryCategories()
        {
            return new List<Category>
            {
                new Category { Name = "Breakfast", Slug = StringHelper.GenerateSlug("Breakfast") },
                new Category { Name = "Main Course", Slug = StringHelper.GenerateSlug("Main Course") },
                new Category { Name = "Desserts", Slug = StringHelper.GenerateSlug("Desserts") },
                new Category { Name = "Healthy Food", Slug = StringHelper.GenerateSlug("Healthy Food") },
                new Category { Name = "Beverages", Slug = StringHelper.GenerateSlug("Beverages") },
                new Category { Name = "Vegetarian", Slug = StringHelper.GenerateSlug("Vegetarian") }
            };
        }

        public static PostAdminDetailsDto CreatePostAdminDetailsDto(Post post)
        {
            return new PostAdminDetailsDto(
                post.Id,
                post.Title,
                post.Description,
                post.Content,
                post.Author,
                post.ImageUrl,
                post.Slug,
                post.MetaTitle,
                post.MetaDescription,
                post.CategoryId,
                post.CreatedAt,
                post.UpdatedAt
            );
        }

        public static PostCreateDto GetPostCreateDto(
            string title = "Test Post Title",
            string slug = "test-post-title",
            string content = "This is the content of the test post.",
            int categoryId = 1)
        {
            return new PostCreateDto
            {
                Title = title,
                Slug = slug,
                Content = content,
                Description = "Test Post Description",
                Author = "Test Author",
                ImageUrl = "http://example.com/image.jpg",
                MetaTitle = "Test Meta Title",
                MetaDescription = "Test Meta Description",
                CategoryId = categoryId
            };
        }

        public static PostUpdateDto GetPostUpdateDto(
            string title = "Updated Post Title",
            string slug = "updated-post-title",
            string content = "Updated content of the post.",
            int categoryId = 1)
        {
            return new PostUpdateDto
            {
                Title = title,
                Slug = slug,
                Content = content,
                Description = "Updated Description",
                Author = "Updated Author",
                ImageUrl = "http://example.com/image.jpg",
                MetaTitle = "Updated Meta Title",
                MetaDescription = "Updated Meta Description",
                CategoryId = categoryId
            };
        }

        public static PostDetailsDto GetPostDetailsDto(
        string slug = "test-post-slug",
        string categorySlug = "test-category-slug",
        string title = "Test Post Title")
        {
            return new PostDetailsDto(
                Id: 1,
                Title: title,
                Description: "Test Description for SEO and social preview.",
                Content: "<h1>Main Content</h1><p>Full post content goes here.</p>",
                Author: "Admin",
                ImageUrl: "https://example.com/images/post.jpg",
                Slug: slug,
                MetaTitle: "SEO Meta Title",
                MetaDescription: "SEO Meta Description",
                Category: "Test Category Name",
                CategorySlug: categorySlug,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: null,
                CommentCount: 5
            );
        }

        public static PostRequestBySlug CreatePostRequest(
        string category = "pasta",
        string slug = "classic-carbonara")
        {
            return new PostRequestBySlug
            {
                Category = category,
                Slug = slug
            };
        }

        public static CommentCreateDto CreateCommentRequest(string content = "Default test content", int? parentId = null) =>
        new() { Content = content };

        public static CommentCreatedDto CreateCommentResponse(
            int id = 1,
            string author = "Bob",
            string content = "Default test content",
            string userId = "TestUserId") =>
            new(id, author, content, DateTime.UtcNow, userId);

        public static CommentUpdatedDto UpdateCommentResponse(
            int id = 1,
            string author = "Bob",
            string content = "Default test content",
            string userId = "TestUserId") =>
            new(id, author, content, DateTime.UtcNow, userId, false);
    }
}
