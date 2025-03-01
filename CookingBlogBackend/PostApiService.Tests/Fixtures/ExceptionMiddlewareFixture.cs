﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PostApiService.Interfaces;
using PostApiService.Tests.Mocks;

namespace PostApiService.Tests.Fixtures
{
    public class ExceptionMiddlewareFixture : TestBaseFixture
    {
        private Exception? _exception;

        public ExceptionMiddlewareFixture() : base("", useDatabase: false) { }

        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.RemoveAll(typeof(IPostService));
            services.AddScoped<IPostService>(_ => new PostServiceMock(_exception));

            services.RemoveAll(typeof(ICommentService));
            services.AddScoped<ICommentService>(_ => new CommentServiceMock(_exception));
        }

        public void SetException(Exception exception)
        {
            _exception = exception;
        }
    }
}
