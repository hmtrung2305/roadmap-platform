namespace RoadmapPlatform.Application.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }

    public class EmailNotVerifiedException : Exception
    {
        public string Email { get; }

        public EmailNotVerifiedException(string email) : base("Email has not been verified")
        {
            Email = email;
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
