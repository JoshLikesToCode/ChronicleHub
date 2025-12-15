namespace ChronicleHub.Domain.Exceptions;

/// <summary>
/// Exception thrown when a request conflicts with the current state of the resource.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string resourceName, object key, string reason)
        : base($"Conflict with {resourceName} '{key}': {reason}")
    {
        ResourceName = resourceName;
        Key = key;
        Reason = reason;
    }

    public string? ResourceName { get; }
    public object? Key { get; }
    public string? Reason { get; }
}
