namespace PostApiService.Exceptions
{
    public class RegisterErrorMessages
    {
        public const string InvalidRegistrationData = "Invalid registration data. Please check the provided information.";
        public const string UsernameAlreadyExists = "This username is already taken.";
        public const string EmailAlreadyExists = "Email already exists.";
        public const string ValidationFailed = "Validation failed.";
        public const string CreationFailed = "User creation failed. ";
        public const string ClaimAssignmentFailed = "Failed to assign claim to user.";
        public const string InternalServerError = "Internal server error. Please, try again later.";
    }
}
