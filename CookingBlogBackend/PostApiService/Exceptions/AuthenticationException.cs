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

    public class EmailAlreadyExistsException : UserRegistrationException
    {
        public EmailAlreadyExistsException(string message)
            : base(message)
        {
        }
    }

    public class UserClaimException : UserRegistrationException
    {
        public UserClaimException(string message)
            : base(message)
        {
        }
    }

    public class UserCreationException : UserRegistrationException
    {
        public UserCreationException(string message)
            : base(string.Format(Auth.Registration.Errors.CreationFailed, message))
        {
        }
    }

    public class UserNotFoundException : InvalidOperationException
    {
        public UserNotFoundException(string message) : base(message) { }
    }

    public class TokenGenerationException : Exception
    {
        public TokenGenerationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
