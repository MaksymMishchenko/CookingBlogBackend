using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Models;

namespace PostApiService.Controllers.Filters
{
    public class ValidatePostIdMatchAttribute : ActionFilterAttribute
    {
        public string InvalidPostIdErrorMessage { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {           
            if (context.ActionArguments.TryGetValue("postId", out var postIdObj) && postIdObj is int postId)
            {               
                if (context.ActionArguments.TryGetValue("comment", out var commentObj) && commentObj is Comment comment)
                {                    
                    if (comment.PostId != postId)
                    {
                        context.Result = new BadRequestObjectResult(ApiResponse<Comment>.CreateErrorResponse(
                            InvalidPostIdErrorMessage ?? "PostId in the request body does not match the URL."));
                        return;
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
