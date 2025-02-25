using Microsoft.EntityFrameworkCore;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Infrastructure
{
    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Registers application-specific services and the database context to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to which services will be added.</param>
        /// <param name="connectionString">The connection string used to configure the database context.</param>
        /// <returns>The IServiceCollection with the added services.</returns>
        public static IServiceCollection AddApplicationService
            (this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

            services.AddTransient<IPostService, PostService>();
            services.AddTransient<ICommentService, CommentService>();

            return services;
        }

        /// <summary>
        /// Seeds the database with initial data for posts and comments if no posts are already present.
        /// This method ensures that the database is created and populated with sample data during application startup.
        /// </summary>
        /// <param name="app">The WebApplication instance to configure services and perform seeding.</param>
        /// <returns>The WebApplication instance for method chaining.</returns>
        public static async Task<IApplicationBuilder> SeedDataAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var cntx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await cntx.Database.EnsureDeletedAsync();

                if (await cntx.Database.EnsureCreatedAsync())
                {
                    var posts = new List<Post>
                    {
                        new Post
                        {
                            Title = "First Post",
                            Description = "Description for first post",
                            Content = "This is the content of the first post.",
                            Author = "Peter Jack",
                            CreateAt = DateTime.Now,
                            ImageUrl = "/images/placeholder.jpg",
                            MetaTitle = "Meta title info",
                            MetaDescription = "This is meta description",
                            Slug = "http://localhost:4200/first-post"
                        },
                        new Post
                        {
                            Title = "Second Post",
                            Description = "Description for second post",
                            Content = "This is the content of the second post.",
                            Author = "Jay Way",
                            CreateAt = DateTime.Now,
                            ImageUrl = "/images/placeholder.jpg",
                            MetaTitle = "Meta title info 2",
                            MetaDescription = "This is meta description 2",
                            Slug = "http://localhost:4200/second-post"
                        }
                    };

                    await cntx.Posts.AddRangeAsync(posts);
                    await cntx.SaveChangesAsync();

                    var comments = new List<Comment>
                    {
                        new Comment
                        {
                            Author = "John Doe",
                            Content = "Great post!",
                            CreatedAt = DateTime.Now,
                            PostId = posts[0].PostId
                        },
                        new Comment
                        {
                            Author = "Jane Doe",
                            Content = "I totally agree with this!",
                            CreatedAt = DateTime.Now,
                            PostId = posts[0].PostId
                        },
                        new Comment
                        {
                            Author = "Alice",
                            Content = "This is a comment on the second post.",
                            CreatedAt = DateTime.Now,
                            PostId = posts[1].PostId
                        }
                    };
                    await cntx.Comments.AddRangeAsync(comments);
                    await cntx.SaveChangesAsync();
                }
            }
            return app;
        }
    }
}
