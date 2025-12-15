namespace ChronicleHub.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user is authenticated but lacks permission to access a resource.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You do not have permission to access this resource.")
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
