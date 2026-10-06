namespace VNZ.Service.Exceptions;

/// <summary>
/// Raised when an admin bilingual request contains a field that is not part of
/// the documented request schema. Keeping this as a domain exception makes the
/// response go through the same ApiResponse envelope as every other API error.
/// </summary>
public sealed class BilingualSchemaException : Exception
{
    public BilingualSchemaException(
        string message,
        IEnumerable<string>? fields = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Fields = fields?.Distinct(StringComparer.Ordinal).ToArray()
            ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Fields { get; }
}
