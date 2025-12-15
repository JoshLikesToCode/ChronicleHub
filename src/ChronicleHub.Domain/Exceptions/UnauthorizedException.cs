namespace ChronicleHub.Domain.Exceptions;

/// <summary>
/// Exception thrown when authentication is required but missing or invalid.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Authentication is required to access this resource.")
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
