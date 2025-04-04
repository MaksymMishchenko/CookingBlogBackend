﻿using MockQueryable;
using NSubstitute;
using PostApiService.Models;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPagedPosts()
        {
            // Arrange                                   
            int pageNumber = 1;
            int pageSize = 10;

            var testPosts = TestDataHelper.GetPostsWithComments(count: 25, commentCount: 10);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            var mockRepository = Substitute.For<IRepository<Post>>();
            mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(mockRepository);

            // Act
            var result = await service.GetAllPostsAsync(pageNumber, pageSize);

            // Assert
            Assert.Equal(pageSize, result.Count);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPaginatedPosts_WithoutComments()
        {
            // Arrange                        
            var pageNumber = 2;
            var pageSize = 10;

            var testPosts = TestDataHelper.GetPostsWithComments(count: 25, commentCount: 10);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            var mockRepository = Substitute.For<IRepository<Post>>();
            mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(mockRepository);

            // Act
            var result = await service.GetAllPostsAsync(pageNumber, pageSize, includeComments: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pageSize, result.Count);

            var actualPostIds = result.Select(p => p.PostId).ToList();

            Assert.All(result, post =>
            {
                Assert.NotNull(post);
                Assert.Empty(post.Comments);
            });
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithComments()
        {
            // Arrange
            var postId = 2;
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, commentCount: 3, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            var mockRepository = Substitute.For<IRepository<Post>>();
            mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(mockRepository);

            // Act
            var post = await service.GetPostByIdAsync(postId, includeComments: true);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.NotEmpty(post.Comments);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithoutComments()
        {
            // Arrange
            var postId = 2;
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, generateComments: false, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            var mockRepository = Substitute.For<IRepository<Post>>();
            mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(mockRepository);

            // Act
            var post = await service.GetPostByIdAsync(postId, includeComments: false);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.Empty(post.Comments);
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddPostSuccessfully_WhenPostIsValid()
        {
            // Arrange           
            var newPost = TestDataHelper.GetSinglePost();

            var mockRepository = Substitute.For<IRepository<Post>>();
            mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            mockRepository.AddAsync(newPost, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(newPost));

            var postService = new PostService(mockRepository);

            // Act
            var result = await postService.AddPostAsync(newPost);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPost.Title, result.Title);
            Assert.Equal(newPost.Content, result.Content);
           
            await mockRepository.Received(1)
                .AnyAsync(Arg.Is<Expression<Func<Post, bool>>>(p =>
                    p.Compile()(newPost)), Arg.Any<CancellationToken>());
            
            await mockRepository.Received(1)
                .AddAsync(newPost, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_WhenPostIsUpdatedSuccessfully()
        {
            // Arrange
            var originalPost = TestDataHelper.GetSinglePost();
            var updatedPost = new Post
            {
                PostId = originalPost.PostId,
                Title = "Updated title",
                Description = "Updated description",
                Content = "Updated content",
                ImageUrl = "updated.jpg",
                MetaTitle = "Updated meta",
                MetaDescription = "Updated meta desc",
                Slug = "updated-slug"
            };

            var mockRepository = Substitute.For<IRepository<Post>>();
           
            mockRepository.GetByIdAsync(originalPost.PostId)
                .Returns(Task.FromResult(originalPost));
            
            mockRepository.UpdateAsync(Arg.Any<Post>())
                .Returns(Task.CompletedTask);

            var service = new PostService(mockRepository);

            // Act
            await service.UpdatePostAsync(updatedPost);

            // Assert            
            await mockRepository.Received(1)
                .GetByIdAsync(originalPost.PostId);
            
            await mockRepository.Received(1)
                .UpdateAsync(Arg.Is<Post>(p =>
                    p.PostId == originalPost.PostId &&
                    p.Title == "Updated title" &&
                    p.Description == "Updated description" &&
                    p.Content == "Updated content" &&
                    p.ImageUrl == "updated.jpg" &&
                    p.MetaTitle == "Updated meta" &&
                    p.MetaDescription == "Updated meta desc" &&
                    p.Slug == "updated-slug"));
        }
    }
}