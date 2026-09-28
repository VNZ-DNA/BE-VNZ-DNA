using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace VNZ.Service.Utils.RichTextService;

public sealed class Service : IService
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p",
        "h2",
        "h3",
        "strong",
        "em",
        "u",
        "s",
        "ul",
        "ol",
        "li",
        "blockquote",
        "a",
        "br"
    };

    private static readonly Regex TagTokenRegex = new(
        @"<!--.*?-->|</?[A-Za-z][^>]*>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex TagRegex = new(
        @"^<(?<closing>/)?(?<name>[A-Za-z][A-Za-z0-9]*)(?<attributes>.*?)(?<selfClosing>/?)>$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex AttributeRegex = new(
        @"(?<name>[A-Za-z_:][A-Za-z0-9_.:-]*)\s*(?:=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)'|(?<unquoted>[^\s""'=<>`]+)))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]*>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    public string? Sanitize(string? value, bool allowLinks)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var result = new StringBuilder();
        var lastIndex = 0;

        foreach (Match tokenMatch in TagTokenRegex.Matches(value))
        {
            AppendEncodedText(result, value[lastIndex..tokenMatch.Index]);
            result.Append(SanitizeTag(tokenMatch.Value, allowLinks));
            lastIndex = tokenMatch.Index + tokenMatch.Length;
        }

        AppendEncodedText(result, value[lastIndex..]);

        var sanitized = result.ToString().Trim();
        return string.IsNullOrWhiteSpace(ToPlainText(sanitized))
            ? null
            : sanitized;
    }

    public string ToPlainText(string value)
    {
        var withoutTags = HtmlTagRegex.Replace(value, " ");
        return WebUtility.HtmlDecode(withoutTags);
    }

    private static string SanitizeTag(string token, bool allowLinks)
    {
        if (token.StartsWith("<!--", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var tagMatch = TagRegex.Match(token);
        if (!tagMatch.Success)
        {
            return string.Empty;
        }

        var tagName = tagMatch.Groups["name"].Value.ToLowerInvariant();
        if (!AllowedTags.Contains(tagName) || (tagName == "a" && !allowLinks))
        {
            return string.Empty;
        }

        if (tagMatch.Groups["closing"].Success)
        {
            return $"</{tagName}>";
        }

        if (tagName == "br")
        {
            return "<br>";
        }

        if (tagName != "a")
        {
            return $"<{tagName}>";
        }

        var href = GetAttributeValue(tagMatch.Groups["attributes"].Value, "href");
        if (href is null || !IsHttpsUrl(href))
        {
            return "<a>";
        }

        return $"<a href=\"{WebUtility.HtmlEncode(href)}\">";
    }

    private static string? GetAttributeValue(string attributes, string attributeName)
    {
        foreach (Match attributeMatch in AttributeRegex.Matches(attributes))
        {
            var name = attributeMatch.Groups["name"].Value;
            if (!string.Equals(name, attributeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (attributeMatch.Groups["double"].Success)
            {
                return attributeMatch.Groups["double"].Value;
            }

            if (attributeMatch.Groups["single"].Success)
            {
                return attributeMatch.Groups["single"].Value;
            }

            if (attributeMatch.Groups["unquoted"].Success)
            {
                return attributeMatch.Groups["unquoted"].Value;
            }

            return null;
        }

        return null;
    }

    private static bool IsHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendEncodedText(StringBuilder result, string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        var decodedText = WebUtility.HtmlDecode(value);
        result.Append(WebUtility.HtmlEncode(decodedText));
    }
}
