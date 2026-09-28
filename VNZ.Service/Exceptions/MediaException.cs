namespace VNZ.Service.Exceptions;

public class MediaException : Exception
{
    public MediaException(string code, string message, params string[] fields)
        : base(message)
    {
        Code = code;
        Fields = fields;
    }

    public MediaException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Fields = Array.Empty<string>();
    }

    public string Code { get; }
    public IReadOnlyList<string> Fields { get; }
}
