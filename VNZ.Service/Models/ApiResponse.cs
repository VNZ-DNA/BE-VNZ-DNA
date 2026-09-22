namespace VNZ.Service.Models;

public class ApiResponse
{
    public bool IsSuccess { get; init; }
    public required string Message { get; init; }
    public object? Data { get; init; }
    public object? Errors { get; init; }
    public string? TraceId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
}

public static class ResponseBuilder
{
    public static ApiResponse SuccessResponse(
        object? data,
        string message,
        string? traceId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = message,
            Data = data,
            Errors = null,
            TraceId = traceId,
            TimestampUtc = DateTimeOffset.UtcNow
        };
    }

    public static ApiResponse ErrorResponse(
        object? errors,
        string message,
        string? traceId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new ApiResponse
        {
            IsSuccess = false,
            Message = message,
            Data = null,
            Errors = errors,
            TraceId = traceId,
            TimestampUtc = DateTimeOffset.UtcNow
        };
    }
}
