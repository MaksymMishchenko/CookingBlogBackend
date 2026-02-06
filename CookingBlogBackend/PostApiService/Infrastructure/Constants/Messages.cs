namespace PostApiService.Infrastructure.Constants
{
    public static class Messages
    {
        public static class PostM
        {
            public static class Errors
            {
                public const string PostNotFound = "Post not found.";
                public const string PostNotFoundCode = "POST_NOT_FOUND";
                public const string SlugAndCategoryRequired = "Slug and category are required and must be valid.";
                public const string SlugAndCategoryRequiredCode = "SLUG_AND_CATEGORY_REQUIRED";
                public const string PostNotFoundByPath = "Post not found or category mismatch for slug: {Slug} in category: {Category}";
                public const string PostNotFoundByPathCode = "POST_NOT_FOUND_OR_CATEGORY_MISMATCH";
                public const string PostTitleOrSlugAlreadyExist = "A post with the same Title '{0}' or Slug {1} already exists.";
                public const string PostAlreadyExistCode = "POST_OR_SLUG_ALREADY_EXISTS";
                public const string CategoryNotFoundCode = "CATEGORY_NOT_FOUND";
                public const string AddPostFailed = "Failed to add post with title '{0}'.";
                public const string NoPostsFound = "No posts found for the requested page.";
                public const string Empty = "Post content cannot be empty.";
                public const string EmptyCode = "POST_CONTENT_IS_EMPTY";
            }

            public static class Success
            {
                public const string SearchResultsFound = "Found {1} posts matching your search '{0}'.";
                public const string SearchNoResults = "No posts found matching your search '{0}'.";
                public const string PostAddedSuccessfully = "Post added successfully.";
                public const string PostUpdatedSuccessfully = "Post updated successfully.";
                public const string PostDeletedSuccessfully = "Post deleted successfully.";
            }
        }

        public static class CategoryM
        {
            public static class Errors
            {
                public const string CategoryNotFound = "Category was not found.";
                public const string CategoryNotFoundCode = "CATEGORY_NOT_FOUND";
                public const string CategoryOrSlugExists = "Category with name '{0}' or slug '{1}' already exists.";
                public const string CategoryOrSlugExistsCode = "CATEGORY_OR_SLUG_EXISTS";
                public const string CannotDeleteCategoryWithPosts = "Cannot delete category with posts";
                public const string CannotDeleteCategoryWithPostsCode = "CATEGORY_DELETE_BLOCKED";
            }

            public static class Success
            {
                public const string CategoryAddedSuccessfully = "Category added successfully.";
                public const string CategoryUpdatedSuccessfully = "Category updated successfully.";
                public const string CategoryDeletedSuccessfully = "Category deleted successfully.";
            }
        }

        public static class CommentM
        {
            public static class Errors
            {
                public const string NotFound = "Comment not found.";
                public const string NotFoundCode = "COMMENT_NOT_FOUND";
                public const string AccessDenied = "You do not have permission to manage this comment.";
                public const string AccessDeniedCode = "COMMENT_ACCESS_DENIED";
                public const string Empty = "Comment content cannot be empty.";
                public const string EmptyCode = "COMMENT_IS_EMPTY";
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
                    public const string InvalidCredentialsErrorCode = "INVALID_CREDENTIALS";
                    public const string UnauthorizedAccess = "Unauthorized Access";
                    public const string UnauthorizedAccessCode = "UNAUTHORIZED";                    
                }

                public static class Success
                {
                    public const string LoginSuccess = "User logged in successfully";
                }
            }

            public static class Registration
            {
                public static class Errors
                {
                    public const string InvalidRegistrationData = "Invalid registration data. Please check the provided information.";
                    public const string InvalidRegistrationDataCode = "REG_INVALID_DATA";
                    public const string DefaultRegistrationError = "An error occurred during registration. Please check your data or try again later.";                   
                    public const string DefaultRegistrationErrorCode = "REGISTRATION_FAILED";
                    public const string UserAlreadyExists = "Username or email is already taken.";
                    public const string UserAlreadyExistsCode = "REG_USER_ALREADY_EXISTS";                   
                    public const string ClaimAssignmentFailed = "Failed to assign claim to user.";
                    public const string ClaimAssignmentFailedCode = "REG_CLAIM_FAILED";
                }

                public static class Success
                {
                    public const string RegisterOk = "User registered successfully.";
                }
            }

            public static class Token
            {
                public static class Errors
                {
                    public const string SecretKeyNullOrEmpty = "Secret key cannot be null or empty.";
                    public const string TokenExpirationInvalid = "Token expiration time must be greater than zero.";                    
                    public const string GenerationFailed = "An unexpected error occurred.";
                }
            }
        }

        public static class Global
        {
            public static class Validation
            {
                public const string Required = "{0} is required.";
                public const string LengthRange = "{0} must be between {2} and {1} characters.";
                public const string MaxLength = "{0} cannot exceed {1} characters.";
                public const string InvalidUrl = "Invalid URL format.";
                public const string SlugFormat = "Slug must only contain lowercase letters, numbers, and hyphens.";
                public const string InvalidCategory = "Please select a valid category.";
                public const string InvalidId = "The provided identifier must be greater than 0.";
                public const string InvalidPageParameters = "Parameter must be greater than 0.";
                public const string PageSizeExceeded = "Items per page size exceeds the allowed maximum.";
                public const string ValidationFailed = "Validation Failed.";
                public const string InvalidEmail = "Invalid email address.";

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
                public const string DatabaseCriticalError = "A technical problem occurred with the data storage. Please try again later.";
                public const string DbUpdateError = "Database error occurred while processing data.";
                public const string Timeout = "The request timed out.";
                public const string DatabaseError = "Database error.";
                public const string RequestCancelled = "The operation was canceled.";
            }
        }
    }
}

