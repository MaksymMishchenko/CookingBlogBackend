using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Tests.Mocks
{
    public class CommentServiceMock : ICommentService
    {
        private readonly Exception? _exception;

        public CommentServiceMock(Exception? exception = null)
        {
            _exception = exception;
        }

        public Task AddCommentAsync(int postId, Comment comment)
        {
            if (_exception != null)
                throw _exception;
            return Task.CompletedTask;
        }

        public Task UpdateCommentAsync(int commentId, EditCommentModel comment)
        {
            if (_exception != null)
                throw _exception;
            return Task.CompletedTask;
        }

        public Task DeleteCommentAsync(int commentId)
        {
            if (_exception != null)
                throw _exception;
            return Task.CompletedTask;
        }
    }
}
