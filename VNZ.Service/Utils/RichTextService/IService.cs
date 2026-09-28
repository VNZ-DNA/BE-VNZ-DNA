namespace VNZ.Service.Utils.RichTextService;

public interface IService
{
    string? Sanitize(string? value, bool allowLinks);

    string ToPlainText(string value);
}
