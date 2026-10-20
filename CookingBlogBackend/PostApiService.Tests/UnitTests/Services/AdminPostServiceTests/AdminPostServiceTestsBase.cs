using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services.AdminPostServiceTests
{
    public class AdminPostServiceTestsBase
    {
        protected readonly IPostRepository _mockRepository;
        protected readonly IWebContext _mockWebContext;
        protected readonly IHtmlSanitizationService _mockSanitizationService;
        protected readonly IPublicCategoryService _mockCategoryService;
        protected readonly IFileStorageService _mockFileStorageService;
        protected readonly AdminPostService _adminPostService;
        protected const string OldPreviewUrl = "https://res.cloudinary.com/x/image/upload/v1/posts/old-abc123.webp";
        protected const string NewPreviewUrl = "https://res.cloudinary.com/x/image/upload/v2/posts/new-def456.webp";

        public AdminPostServiceTestsBase()
        {
            _mockRepository = Substitute.For<IPostRepository>();
            _mockWebContext = Substitute.For<IWebContext>();
            _mockSanitizationService = Substitute.For<IHtmlSanitizationService>();
            _mockCategoryService = Substitute.For<IPublicCategoryService>();
            _mockFileStorageService = Substitute.For<IFileStorageService>();
            _adminPostService = new AdminPostService(_mockRepository,
                _mockWebContext, _mockSanitizationService, _mockCategoryService, _mockFileStorageService);
        }

        protected Post ArrangeSuccessfulUpdate(int postId, string? existingPreviewUrl)
        {
            var post = new Post
            {
                Id = postId,
                CategoryId = 99,
                AuthorId = "user-id",
                PreviewImageUrl = existingPreviewUrl!
            };

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.GetByIdWithAuthorsAsync(postId, Arg.Any<CancellationToken>()).Returns(post);
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

            return post;
        }
    }
}
