using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Tests.Mocks
{
    public class PostServiceMock : IPostService
    {
        private readonly Exception? _exception;

        public PostServiceMock(Exception? exception = null)
        {
            _exception = exception;
        }

        public Task<List<Post>> GetAllPostsAsync(int pageNumber, int pageSize, int commentPageNumber = 1, int commentsPerPage = 10, bool includeComments = true, CancellationToken cancellationToken = default)
        {
            if (_exception != null)
                throw _exception;

            return Task.FromResult(new List<Post> { new Post { Title = "Mocked Post" } });
        }

        public Task<Post> GetPostByIdAsync(int postId, bool includeComments = true)
        {
            if (_exception != null)
                throw _exception;

            return Task.FromResult(new Post { PostId = postId, Title = "Mocked Post" });
        }

        public Task<Post> AddPostAsync(Post post)
        {
            if (_exception != null)
                throw _exception;

            return Task.FromResult(new Post { Title = "Mocked Post" });
        }

        public Task UpdatePostAsync(Post post)
        {
            if (_exception != null)
                throw _exception;

            return Task.CompletedTask;
        }

        public Task DeletePostAsync(int postId)
        {
            if (_exception != null)
                throw _exception;

            return Task.CompletedTask;
        }
    }
}
