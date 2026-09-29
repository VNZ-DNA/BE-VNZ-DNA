namespace VNZ.Service.Utils.RichTextService;

public interface IService
{
    string? Sanitize(string? value, bool allowLinks);

    string? SanitizeNewsSummary(string? value);

    string? SanitizeNewsContent(string? value);

    string ToPlainText(string value);
}
