namespace VNZ.Service.Exceptions;

public class JobApplicationException : Exception
{
    public JobApplicationException(string code, string message, params string[] fields)
        : base(message)
    {
        Code = code;
        Fields = fields;
    }

    public JobApplicationException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Fields = Array.Empty<string>();
    }

    public string Code { get; }
    public IReadOnlyList<string> Fields { get; }
}
