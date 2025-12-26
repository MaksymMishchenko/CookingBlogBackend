namespace PostApiService.Infrastructure.Constants
{
    public static class Messages
    {
        public static class PostM
        {
            public static class Errors
            {
                public const string PostNotFound = "Post with ID {0} was not found.";
                public const string PostAlreadyExist = "A post with the same Title '{0}' already exists.";
                public const string AddPostFailed = "Failed to add post with title '{0}'.";
                public const string UpdatePostFailed = "Failed to update post with title '{0}'.";
                public const string DeletePostFailed = "Failed to delete post with title '{0}'.";
                public const string NoPostsFound = "No posts found for the requested page.";
            }

            public static class Success
            {
                public const string PostRetrievedSuccessfully = "Post with ID {0} retrieved successfully.";
                public const string NoPostsAvailableYet = "No posts available yet";
                public const string NoPostsFound = "No posts found matching your search: '{0}'.";
                public const string PostsRetrievedSuccessfully = "Successfully retrieved {0} posts.";
                public const string PostAddedSuccessfully = "Post added successfully.";
                public const string PostUpdatedSuccessfully = "Post with ID {0} updated successfully.";
                public const string PostDeletedSuccessfully = "Post with ID {0} deleted successfully.";
            }
        }

        public static class CommentM
        {
            public static class Errors
            {
                public const string CommentNotFound = "Comment with ID {0} was not found.";
                public const string AddCommentFailed = "Failed to add the comment to the post with ID {0}.";
                public const string UpdateCommentFailed = "Failed to update the comment with ID {0}.";
                public const string DeleteCommentFailed = "Failed to delete the comment with ID {0}.";
            }

            public static class Success
            {
                public const string CommentAddedSuccessfully = "Comment added successfully.";
                public const string CommentUpdatedSuccessfully = "Comment updated successfully.";
                public const string CommentDeletedSuccessfully = "Comment deleted successfully.";
            }
        }

        public static class Auth
        {
            public static class LoginM
            {
                public static class Errors
                {
                    public const string InvalidCredentials = "Invalid username or password. Please check your credentials and try again.";
                    public const string UnauthorizedAccess = "Unauthorized Access";
                    public const string UserNotFound = "User Not Found";
                }

                public static class Success
                {
                    public const string LoginSuccess = "User with username '{0}' logged in successfully";
                }
            }

            public static class Registration
            {
                public static class Errors
                {
                    public const string InvalidRegistrationData = "Invalid registration data. Please check the provided information.";
                    public const string UsernameAlreadyExists = "This username is already taken.";
                    public const string EmailAlreadyExists = "Email already exists.";
                    public const string CreationFailed = "User creation failed. ";
                    public const string ClaimAssignmentFailed = "Failed to assign claim to user.";                    
                }

                public static class Success
                {
                    public const string RegisterOk = "User with username {0} registered successfully.";
                }
            }

            public static class Token
            {
                public static class Errors
                {
                    public const string SecretKeyNullOrEmpty = "Secret key cannot be null or empty.";
                    public const string TokenExpirationInvalid = "Token expiration time must be greater than zero.";                    
                    public const string TokenGenerationFailed = "An error occurred while generating the token.";
                    public const string GenerationFailed = "An unexpected error occurred.";
                }
            }
        }

        public static class Global
        {
            public static class Validation
            {
                public const string InvalidId = "The provided identifier must be greater than 0.";
                public const string InvalidPageParameters = "Parameter must be greater than 0.";
                public const string PageSizeExceeded = "Items per page size exceeds the allowed maximum.";
                public const string ValidationFailed = "Validation Failed.";
                public const string InvalidNumberFormat = "The value '{0}' is not a valid number.";
                public const string SearchQueryForbiddenCharacters =
                    "Search query contains forbidden characters. Use only letters, numbers, spaces, dots or hyphens.";
                public const string SearchQueryMustContainLetterOrDigit =
                    "Search query must contain at least one letter or digit.";
                public const string UnknownValue = "unknown";
                public const string RequestBodyRequired = "Request body cannot be empty.";
                public const string UnexpectedErrorException = "An error occurred while processing your request.";
                public const string SearchQueryRequired = "Search query string is required and cannot be empty.";
                public const string SearchQueryTooShort = "Query string must be at least {0} characters long.";
                public const string SearchQueryTooLong = "Query string cannot exceed {0} characters.";
            }

            public static class System
            {
                public const string DbUpdateError = "Database error occurred while processing data.";
                public const string Timeout = "The request timed out.";
                public const string DatabaseError = "Database error.";  // SqlException
                public const string RequestCancelled = "The operation was canceled."; 
            }
        }
    }
}

