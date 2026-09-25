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
            string[] authorIds,
            bool useNewSeed = false,
            bool generateComments = true,
            int commentCount = 1,
            bool generateIds = false,
            Category? forcedCategory = null)
        {
            var posts = GetPostFaker(useNewSeed, categories!, authorIds, generateComments, commentCount, generateIds, forcedCategory).Generate(count);

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
            string[] authorIds,
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
                .RuleFor(p => p.AuthorId, f => f.PickRandom(authorIds))
                .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
                .RuleFor(p => p.MetaTitle, f => f.Lorem.Sentence(2))
                .RuleFor(p => p.MetaDescription, f => f.Lorem.Sentence(3).ClampLength(50, 200))
                .RuleFor(p => p.Slug, f => f.Lorem.Slug())
                .RuleFor(p => p.IsActive, f => true)
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
                ImageUrl = post.ImageUrl,
                MetaTitle = post.MetaTitle,
                MetaDescription = post.MetaDescription,
                Slug = post.Slug,
                CategoryId = post.CategoryId,
                IsActive = post.IsActive
            };
        }

        public static void AssertCategoryAsync(Category expectedCategory, CategoryDto actualDto)
        {
            Assert.NotNull(actualDto);
            Assert.Equal(expectedCategory.Id, actualDto.Id);
            Assert.Equal(expectedCategory.Name, actualDto.Name);
        }        

        public static Post GetSinglePostWithCategoryId(int categoryId, string authorId = "test-admin-id")
        {
            return new Post
            {
                Title = "Valid Test Title",
                Slug = "valid-test-slug",
                Content = "Full content of the test post.",
                Description = "Brief description",
                AuthorId = authorId,
                ImageUrl = "http://example.com/image.jpg",
                MetaTitle = "Meta Title",
                MetaDescription = "Meta Description",
                CategoryId = categoryId
            };
        }

        public static List<Post> GetPostsWithComments(ICollection<Category> categories,
            string userId = "testContId",
            string authorId = "test-admin-id")
        {
            var beverages = categories.First(c => c.Name == "Beverages");
            var vegetarian = categories.First(c => c.Name == "Vegetarian");
            var desserts = categories.First(c => c.Name == "Desserts");
            var breakfast = categories.First(c => c.Name == "Breakfast");
            var healthyFood = categories.First(c => c.Name == "Healthy Food");

            List<Comment> CreateDefaultComments(int postId) => new()
            {
                new Comment { Content = "Post comment content 1", PostId = postId, UserId = userId },
                new Comment { Content = "Post comment content 2", PostId = postId, UserId = userId },
                new Comment { Content = "Post comment content 3", PostId = postId, UserId = userId }
            };

            return new List<Post> {
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 1",
                    Description = "Description lorem ipsum dolor sit amet 1",
                    AuthorId = authorId,
                    Content = "Simple comtemt lorem ipsum dolor sit amet 1",
                    ImageUrl = "http://img-1.com",
                    MetaTitle = "Meta title dolor sit amet 1",
                    MetaDescription = "Meta lorem ipsum dolor 1",
                    Slug = "post-slug-1",
                    Category = beverages,
                    CategoryId = beverages.Id,
                    Comments = CreateDefaultComments(1)
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 2",
                    Description = "Description lorem ipsum dolor sit amet 2",
                    AuthorId = authorId,
                    Content = "Simple comtemt lorem ipsum dolor sit amet 2",
                    ImageUrl = "http://img-2.com",
                    MetaTitle = "Meta title dolor sit amet 2",
                    MetaDescription = "Meta lorem ipsum dolor 2",
                    Slug = "post-slug-2",
                    Category = vegetarian,
                    CategoryId = vegetarian.Id,
                    Comments = CreateDefaultComments(2)
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 3",
                    Description = "Description lorem ipsum dolor sit amet 3",
                    AuthorId = authorId,
                    Content = "Simple comtemt lorem ipsum dolor sit amet 3",
                    ImageUrl = "http://img-3.com",
                    MetaTitle = "Meta title dolor sit amet 3",
                    MetaDescription = "Meta lorem ipsum dolor 3",
                    Slug = "post-slug-3",
                    Category = desserts,
                    CategoryId = desserts.Id,
                    Comments = CreateDefaultComments(3)
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 4",
                    Description = "Description lorem ipsum dolor sit amet 4",
                    AuthorId = authorId,
                    Content = "Simple comtemt lorem ipsum dolor sit amet 4",
                    ImageUrl = "http://img-4.com",
                    MetaTitle = "Meta title dolor sit amet 4",
                    MetaDescription = "Meta lorem ipsum dolor 4",
                    Slug = "post-slug-4",
                    Category = breakfast,
                    CategoryId = breakfast.Id,
                    Comments = CreateDefaultComments(4)
                },
                new Post {
                    Title = "Title Lorem ipsum dolor sit amet 5",
                    Description = "Description lorem ipsum dolor sit amet 5",
                    AuthorId = authorId,
                    Content = "Simple comtemt lorem ipsum dolor sit amet 5",
                    ImageUrl = "http://img-5.com",
                    MetaTitle = "Meta title dolor sit amet 5",
                    MetaDescription = "Meta lorem ipsum dolor 5",
                    Slug = "post-slug-5",
                    Category = healthyFood,
                    CategoryId = healthyFood.Id,
                    Comments = CreateDefaultComments(5)
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

            yield return new object[] { null!, 5, null!, 1, "Beverages" };
            yield return new object[] { null!, 5, true, 1, "Beverages" };
            yield return new object[] { null!, 5, false, 0, "Beverages" };

            yield return new object[] { "Lorem", 3, true, 1, "Desserts" };
            yield return new object[] { "Lorem", 6, false, 1, "Vegetarian" };
        }

        public static List<Category> GetCulinaryCategories()
        {
            return new List<Category>
            {
                new Category { Id = 1, Name = "Breakfast", Slug = StringHelper.GenerateSlug("Breakfast") },
                new Category { Id = 2, Name = "Main Course", Slug = StringHelper.GenerateSlug("Main Course") },
                new Category { Id = 3, Name = "Desserts", Slug = StringHelper.GenerateSlug("Desserts") },
                new Category { Id = 4, Name = "Healthy Food", Slug = StringHelper.GenerateSlug("Healthy Food") },
                new Category { Id = 5, Name = "Beverages", Slug = StringHelper.GenerateSlug("Beverages") },
                new Category { Id = 6, Name = "Vegetarian", Slug = StringHelper.GenerateSlug("Vegetarian") }
            };
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
                ImageUrl = "http://example.com/image.jpg",
                MetaTitle = "Updated Meta Title",
                MetaDescription = "Updated Meta Description",
                CategoryId = categoryId,
                IsActive = true
            };
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

        public static Post ToEntity(PostCreateDto dto, string sanitizedContent, string userId)
        {
            return new Post
            {
                Id = 1,
                Title = dto.Title,
                Description = dto.Description,
                Content = sanitizedContent,
                Slug = dto.Slug,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                AuthorId = userId,
                IsActive = true
            };
        }
    }
}
