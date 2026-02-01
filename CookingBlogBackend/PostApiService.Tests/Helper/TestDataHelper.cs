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
                    if (!generateComments)
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

        public static List<Post> GeneratePostsWithKeyword
            (string keyword, ICollection<Category> categories, int count, bool generateIds = false)
        {
            int postId = 1;
            var posts = new List<Post>();
            var faker = new Faker<Post>("en")
                .RuleFor(p => p.Id, _ => 0)
                .RuleFor(p => p.Author, _ => "Author")
                .RuleFor(p => p.ImageUrl, _ => "image.jpeg")
                .RuleFor(p => p.MetaTitle, _ => "Meta title")
                .RuleFor(p => p.MetaDescription, _ => "Meta description")
                .RuleFor(p => p.Slug, f => f.Lorem.Slug())
                .RuleFor(p => p.CreatedAt, f => f.Date.Past(1))
                .RuleFor(p => p.Category, f => f.PickRandom(categories))
                .RuleFor(p => p.CategoryId, (f, p) => p.Category.Id);

            for (int i = 0; i < count; i++)
            {
                var post = faker.Generate();

                if (generateIds)
                {
                    post.Id = postId++;
                }

                post.Title = $"{keyword} Post Title {i}";
                post.Description = $"A detailed description about {keyword}.";
                post.Content = $"A detailed content about {keyword}.";

                posts.Add(post);
            }
            return posts;
        }

        public static List<Post> GetPostsForOrLogic(ICollection<Category> categories)
        {
            var baseTime = DateTime.UtcNow;

            return new List<Post>
            {
                new Post
                {
                    Title = "Chili Recipe",
                    Description = "Simple cooking guide",
                    Content = "This is a long content about cooking without keywords.",
                    Author = "Chef John",
                    CreatedAt = baseTime.AddHours(-1),
                    ImageUrl = "https://example.com/image1.jpg",
                    MetaTitle = "Chili Recipe Title",
                    MetaDescription = "Meta description about chili",
                    Slug = "chili-recipe-title",
                    Category = categories.First(c => c.Name == "Beverages")
                },
                new Post
                {
                    Title = "Secret Dish",
                    Description = "The Best Chili ever made in Texas",
                    Content = "Try to guess what is inside this amazing secret dish.",
                    Author = "Jane Doe",
                    CreatedAt = baseTime.AddHours(-2),
                    ImageUrl = "https://example.com/image2.jpg",
                    MetaTitle = "Secret Dish Meta",
                    MetaDescription = "Meta description for secret dish",
                    Slug = "secret-dish-desc",
                    Category = categories.First(c => c.Name == "Desserts")
                },
                new Post
                {
                    Title = "Spicy Ingredient",
                    Description = "Information about spices",
                    Content = "Actually, the secret ingredient is Chili, you should try it.",
                    Author = "Admin",
                    CreatedAt = baseTime.AddHours(-3),
                    ImageUrl = "https://example.com/image3.jpg",
                    MetaTitle = "Ingredient Meta",
                    MetaDescription = "Meta description for ingredient",
                    Slug = "spicy-ingredient-content",
                    Category = categories.First(c => c.Name == "Vegetarian")
                },
                new Post
                {
                    Title = "Healthy Food",
                    Description = "Fresh Green Salad",
                    Content = "No spicy things in this recipe, only vegetables.",
                    Author = "Healthy Life",
                    CreatedAt = baseTime.AddHours(-4),
                    ImageUrl = "https://example.com/image4.jpg",
                    MetaTitle = "Salad Meta",
                    MetaDescription = "Meta description for salad",
                    Slug = "healthy-food-salad",
                    Category = categories.First(c => c.Name == "Breakfast")
                }
            };
        }

        public static List<PostListDto> GetPostListDtos(int count, ICollection<Category> categories)
        {
            var posts = GetPostsWithComments(count, categories, generateIds: true);
            var mapper = PostMappingExtensions.ToDtoExpression.Compile();

            return posts.Select(mapper).ToList();
        }

        public static List<CategoryDto> GetCategoryListDtos(ICollection<Category> categories)
        {
            return categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList();
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
            Assert.Equal(expectedPost.Category.Name, actualDto.Category);
            Assert.Equal(expectedPost.Description, actualDto.Description);
            Assert.Equal(expectedPost.CreatedAt, actualDto.CreatedAt);
            Assert.Equal(expectedPost.UpdatedAt, actualDto.UpdatedAt);
            Assert.Equal(expectedCommentCount, actualDto.CommentsCount);
        }

        public static void AssertSearchPostsWithTotalCountAsync(Post expectedPost, SearchPostListDto actualDto)
        {
            Assert.NotNull(actualDto);
            Assert.Equal(expectedPost.Id, actualDto.Id);
            Assert.Equal(expectedPost.Title, actualDto.Title);
            Assert.Equal(expectedPost.Slug, actualDto.Slug);
            Assert.Equal(expectedPost.Author, actualDto.Author);
            Assert.Equal(expectedPost.Author, actualDto.Author);
        }

        public static void AssertCategoryAsync(Category expectedCategory, CategoryDto actualDto)
        {
            Assert.NotNull(actualDto);
            Assert.Equal(expectedCategory.Id, actualDto.Id);
            Assert.Equal(expectedCategory.Name, actualDto.Name);
        }

        public static List<Post> GetSearchedPost(ICollection<Category> categories)
        {
            return new List<Post>
            {
                new Post
                {
                    Id = 1,
                    Title = "Ultimate Classic Chili Cheeseburger Recipe",
                    Slug = "ultimate-chili-cheeseburger",
                    Description = "How to grill the perfect juicy patty and melt the cheese.",
                    Content = "Tips for brioche buns, sharp cheddar, and secret sauce.",
                    CreatedAt = DateTime.Now.AddHours(-10),
                    Author = "Chef Mike",
                    Category = categories.First(c => c.Name == "Breakfast")
                },
                new Post
                {
                    Id = 2,
                    Title = "Easy Homemade Chili Cheese Dog Sauce",
                    Slug = "easy-chili-cheese-dog",
                    Description = "Stretch and fold technique for thin, foldable crust.",
                    Content = "High-protein flour, proofing secrets, and oven temps.",
                    CreatedAt = DateTime.Now.AddHours(-1),
                    Author = "Peter",
                    Category = categories.First(c => c.Name == "Healthy Food")
                },
                new Post
                {
                    Id = 3,
                    Title = "Authentic Texas Chili (No Beans)",
                    Slug = "texas-chili-no-beans",
                    Description = "The official recipe for rich, smoky, bean-free Texas chili.",
                    Content = "Using chili powder, beef chuck, and dried peppers for depth.",
                    CreatedAt = DateTime.Now.AddHours(-6),
                    Author = "Sarah",
                    Category = categories.First(c => c.Name == "Beverages")
                },
                new Post
                {
                    Id = 4,
                    Title = "Quick 30-Minute Chicken Tacos",
                    Slug = "quick-chicken-tacos",
                    Description = "Simple seasoning mix and fast pan-searing method.",
                    Content = "Shredded chicken, lime, and fresh cilantro garnish.",
                    CreatedAt = DateTime.Now.AddHours(-4),
                    Author = "Monika",
                    Category = categories.First(c => c.Name == "Vegetarian")
                },
            };
        }

        public static List<Post> GetSearchedPostWithoutIds(ICollection<Category> categories)
        {
            return new List<Post>
            {
                new Post
                {
                    Title = "Ultimate Classic Chili Cheeseburger Recipe",
                    Slug = "ultimate-chili-cheeseburger",
                    Description = "How to grill the perfect juicy patty and melt the cheese.",
                    Content = "Tips for brioche buns, sharp cheddar, and secret sauce.",
                    CreatedAt = DateTime.Now.AddHours(-10),
                    Author = "Chef Mike",
                    MetaTitle = "Ultimate Classic Chili",
                    MetaDescription = "Perfect juicy patty and melt the cheese",
                    ImageUrl = "image1.jpg",
                    Comments = new List<Comment>(),
                    Category = categories.First(c=> c.Name == "Main Course")
                },
                new Post
                {
                    Title = "Easy Homemade Chili Cheese Dog Sauce",
                    Slug = "easy-chili-cheese-dog",
                    Description = "Stretch and fold technique for thin, foldable crust.",
                    Content = "High-protein flour, proofing secrets, and oven temps.",
                    CreatedAt = DateTime.Now.AddHours(-1),
                    Author = "Peter",
                    MetaTitle = "Easy Homemade Chili",
                    MetaDescription = "High-protein flour",
                    ImageUrl = "image2.jpg",
                    Comments = new List<Comment>(),
                    Category = categories.First(c=> c.Name == "Breakfast")
                },
                new Post
                {
                    Title = "Authentic Texas Chili (No Beans)",
                    Slug = "texas-chili-no-beans",
                    Description = "The official recipe for rich, smoky, bean-free Texas chili.",
                    Content = "Using chili powder, beef chuck, and dried peppers for depth.",
                    CreatedAt = DateTime.Now.AddHours(-6),
                    Author = "Sarah",
                    MetaTitle = "Authentic Texas Chili",
                    MetaDescription = "The official recipe for rich",
                    ImageUrl = "image3.jpg",
                    Comments = new List<Comment>(),
                    Category = categories.First(c=> c.Name == "Vegetarian")
                },
                new Post
                {
                    Title = "Quick 30-Minute Chicken Tacos",
                    Slug = "quick-chicken-tacos",
                    Description = "Simple seasoning mix and fast pan-searing method.",
                    Content = "Shredded chicken, lime, and fresh cilantro garnish.",
                    CreatedAt = DateTime.Now.AddHours(-4),
                    Author = "Monika",
                    MetaTitle = "Quick 30-Minute Chicken Tacos",
                    MetaDescription = "Simple seasoning mix",
                    ImageUrl = "image4.jpg",
                    Comments = new List<Comment>(),
                    Category = categories.First(c=> c.Name == "Beverages")
                },
            };
        }

        public static List<SearchPostListDto> GetSearchedPostListDtos(ICollection<Category> categories)
        {
            var posts = GetSearchedPost(categories);

            return posts.Select(p => new SearchPostListDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Content,
                p.Author,
                p.Category.Name,
                p.Category.Slug
                ))
                 .ToList();
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

        public static Post GetSingleCulinaryPost(string? slug = null, string? categoryName = null)
        {
            var categories = GetCulinaryCategories();
            var selectedCategory = categoryName != null
                ? categories.First(c => c.Name == categoryName)
                : categories.First(c => c.Name == "Main Course");

            var faker = GetPostFaker(useNewSeed: true, new List<Category> { selectedCategory },
                                     generateComments: false, commentCount: 0, generateIds: true);

            var post = faker.Generate();

            if (!string.IsNullOrEmpty(slug))
            {
                post.Slug = slug;
            }

            return post;
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

        public static List<Comment> GetListWithComments()
        {
            return new List<Comment> {
                new Comment { PostId = 1, Id = 1, UserId = "testUserId", Content = "This is the test comment 1." },
                new Comment { PostId = 1, Id = 2, UserId = "testUserId", Content = "This is the test comment 2." },
                new Comment { PostId = 1, Id = 3, UserId = "testUserId", Content = "This is the test comment 3." }
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

        public static CommentCreateDto CreateCommentRequest(string content = "Default test content") =>
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
