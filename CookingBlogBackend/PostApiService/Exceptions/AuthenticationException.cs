namespace PostApiService.Exceptions
{
    public class UserRegistrationException : Exception
    {
        public UserRegistrationException(string message) : base(message)
        {
        }
    }

    public class UserAlreadyExistsException : UserRegistrationException
    {
        public UserAlreadyExistsException(string message)
            : base(message)
        {
        }
    }

    public class UserCreationException : UserRegistrationException
    {
        public UserCreationException(string message)
            : base(string.Format(RegisterErrorMessages.CreationFailed, message))
        {
        }
    }
}
