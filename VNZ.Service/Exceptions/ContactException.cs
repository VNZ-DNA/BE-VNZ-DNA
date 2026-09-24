namespace VNZ.Service.Exceptions;

public class ContactException : Exception
{
    public ContactException(string code, string message, params string[] fields)
        : base(message)
    {
        Code = code;
        Fields = fields;
    }

    public string Code { get; }
    public IReadOnlyList<string> Fields { get; }
}
