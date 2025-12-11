namespace ChronicleHub.Api.Contracts.Common;

/// <summary>
/// Consistent API response envelope with metadata support.
/// </summary>
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public ApiMetadata? Metadata { get; init; }

    public static ApiResponse<T> SuccessResult(T data, ApiMetadata? metadata = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Error = null,
            Metadata = metadata
        };
    }

    public static ApiResponse<T> ErrorResult(ApiError error, ApiMetadata? metadata = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Error = error,
            Metadata = metadata
        };
    }
}
