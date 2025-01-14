﻿using PostApiService.Models;

namespace PostApiService.Interfaces
{
    public interface ICommentService
    {        
        Task<bool> AddCommentAsync(int postId, Comment comment);
        Task<bool> UpdateCommentAsync(int commentId, EditCommentModel comment);
        Task<bool> DeleteCommentAsync(int commentId);
    }
}
