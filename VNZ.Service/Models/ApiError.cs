namespace VNZ.Service.Models;

/// <summary>
/// Stable error payload nested in <see cref="ApiResponse.Errors"/>.
/// </summary>
public sealed class ApiError
{
    public required string Code { get; init; }
    public required IReadOnlyList<string> Fields { get; init; }
}
