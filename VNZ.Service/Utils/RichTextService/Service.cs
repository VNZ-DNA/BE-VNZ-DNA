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

    private static readonly HashSet<string> NewsAllowedTags = new(
        AllowedTags
            .Concat(new[] { "figure", "figcaption", "img", "div" }),
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> NewsSummaryAllowedTags = new(
        new[] { "p", "strong", "em", "a", "br" },
        StringComparer.OrdinalIgnoreCase);

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

    public string? SanitizeNewsSummary(string? value)
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
            result.Append(SanitizeTag(
                tokenMatch.Value,
                allowLinks: true,
                NewsSummaryAllowedTags));
            lastIndex = tokenMatch.Index + tokenMatch.Length;
        }

        AppendEncodedText(result, value[lastIndex..]);

        var sanitized = result.ToString().Trim();
        return string.IsNullOrWhiteSpace(ToPlainText(sanitized))
            ? null
            : sanitized;
    }

    public string? SanitizeNewsContent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var result = new StringBuilder();
        var openElements = new List<NewsOpenElement>();
        var lastIndex = 0;

        foreach (Match tokenMatch in TagTokenRegex.Matches(value))
        {
            AppendEncodedText(result, value[lastIndex..tokenMatch.Index]);
            ProcessNewsToken(tokenMatch.Value, openElements, result);
            lastIndex = tokenMatch.Index + tokenMatch.Length;
        }

        AppendEncodedText(result, value[lastIndex..]);

        if (openElements.Count > 0)
        {
            throw new RichTextValidationException(
                "Nội dung bài viết có thẻ HTML chưa được đóng.");
        }

        var sanitized = result.ToString().Trim();
        if (string.IsNullOrWhiteSpace(ToPlainText(sanitized))
            && !sanitized.Contains("<img ", StringComparison.Ordinal))
        {
            return null;
        }

        return sanitized;
    }

    public string ToPlainText(string value)
    {
        var withoutTags = HtmlTagRegex.Replace(value, " ");
        return WebUtility.HtmlDecode(withoutTags);
    }

    private static void ProcessNewsToken(
        string token,
        List<NewsOpenElement> openElements,
        StringBuilder result)
    {
        if (token.StartsWith("<!--", StringComparison.Ordinal))
        {
            return;
        }

        var tagMatch = TagRegex.Match(token);
        if (!tagMatch.Success)
        {
            return;
        }

        var tagName = tagMatch.Groups["name"].Value.ToLowerInvariant();
        if (!NewsAllowedTags.Contains(tagName))
        {
            return;
        }

        var isClosing = tagMatch.Groups["closing"].Success;
        if (isClosing)
        {
            CloseNewsTag(tagName, openElements, result);
            return;
        }

        if (tagName == "br")
        {
            result.Append("<br>");
            return;
        }

        if (tagName == "img")
        {
            AppendNewsImage(tagMatch.Groups["attributes"].Value, openElements, result);
            return;
        }

        if (tagName == "figure")
        {
            OpenNewsFigure(tagMatch.Groups["attributes"].Value, openElements, result);
            return;
        }

        if (tagName == "div")
        {
            OpenNewsGalleryImages(tagMatch.Groups["attributes"].Value, openElements, result);
            return;
        }

        if (tagName == "figcaption")
        {
            OpenNewsCaption(openElements, result);
            return;
        }

        ValidateNewsTextTagParent(tagName, openElements);

        if (tagName == "a")
        {
            var href = GetAttributeValue(tagMatch.Groups["attributes"].Value, "href");
            if (href is not null)
            {
                href = WebUtility.HtmlDecode(href);
            }

            result.Append(href is not null && IsHttpsUrl(href)
                ? $"<a href=\"{WebUtility.HtmlEncode(href)}\">"
                : "<a>");
        }
        else
        {
            result.Append($"<{tagName}>");
        }

        openElements.Add(new NewsOpenElement(tagName));
    }

    private static void CloseNewsTag(
        string tagName,
        List<NewsOpenElement> openElements,
        StringBuilder result)
    {
        if (openElements.Count == 0
            || !string.Equals(
                openElements[^1].TagName,
                tagName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new RichTextValidationException(
                $"Nội dung bài viết có cấu trúc thẻ {tagName} không hợp lệ.");
        }

        var element = openElements[^1];
        if (element.IsGalleryImagesContainer && element.GalleryImageCount != 2)
        {
            throw new RichTextValidationException(
                "Gallery bài viết phải có đúng 2 ảnh.");
        }

        if (element.FigureState is not null)
        {
            var figure = element.FigureState;
            if (figure.ClassName == "news-image"
                && (figure.ImageCount != 1 || figure.HasGalleryImagesContainer))
            {
                throw new RichTextValidationException(
                    "Khối ảnh đơn của bài viết phải có đúng 1 ảnh.");
            }

            if (figure.ClassName == "news-gallery"
                && (!figure.HasGalleryImagesContainer || figure.ImageCount != 2))
            {
                throw new RichTextValidationException(
                    "Gallery bài viết phải có đúng 2 ảnh.");
            }
        }

        openElements.RemoveAt(openElements.Count - 1);
        result.Append($"</{tagName}>");
    }

    private static void OpenNewsFigure(
        string attributes,
        List<NewsOpenElement> openElements,
        StringBuilder result)
    {
        if (openElements.Any(element => element.FigureState is not null))
        {
            throw new RichTextValidationException(
                "Không được lồng các khối ảnh trong nội dung bài viết.");
        }

        var className = GetAttributeValue(attributes, "class");
        className = className is null ? null : WebUtility.HtmlDecode(className).Trim();
        if (className is not ("news-image" or "news-gallery"))
        {
            throw new RichTextValidationException(
                "Khối ảnh bài viết phải có class news-image hoặc news-gallery.");
        }

        result.Append($"<figure class=\"{className}\">");
        openElements.Add(new NewsOpenElement(
            "figure",
            new NewsFigureState(className)));
    }

    private static void OpenNewsGalleryImages(
        string attributes,
        List<NewsOpenElement> openElements,
        StringBuilder result)
    {
        var parent = GetTopElement(openElements);
        var className = GetAttributeValue(attributes, "class");
        className = className is null ? null : WebUtility.HtmlDecode(className).Trim();
        if (parent?.FigureState?.ClassName != "news-gallery"
            || parent.TagName != "figure"
            || className != "news-gallery-images"
            || parent.FigureState.HasGalleryImagesContainer)
        {
            throw new RichTextValidationException(
                "Gallery bài viết phải có một khối news-gallery-images hợp lệ.");
        }

        parent.FigureState.HasGalleryImagesContainer = true;
        result.Append("<div class=\"news-gallery-images\">");
        openElements.Add(new NewsOpenElement("div", isGalleryImagesContainer: true));
    }

    private static void OpenNewsCaption(
        List<NewsOpenElement> openElements,
        StringBuilder result)
    {
        var parent = GetTopElement(openElements);
        if (parent?.FigureState is null || parent.TagName != "figure")
        {
            throw new RichTextValidationException(
                "Caption bài viết phải nằm trực tiếp trong khối ảnh.");
        }

        if (parent.FigureState.CaptionCount > 0)
        {
            throw new RichTextValidationException(
                "Mỗi khối ảnh bài viết chỉ được có một caption.");
        }

        parent.FigureState.CaptionCount++;
        result.Append("<figcaption>");
        openElements.Add(new NewsOpenElement("figcaption"));
    }

    private static void AppendNewsImage(
        string attributes,
        List<NewsOpenElement> openElements,
        StringBuilder result)
    {
        var parent = GetTopElement(openElements);
        var isSingleImage = parent?.FigureState is not null
            && parent.TagName == "figure"
            && parent.FigureState.ClassName == "news-image";
        var isGalleryImage = parent?.IsGalleryImagesContainer == true;
        if (!isSingleImage && !isGalleryImage)
        {
            throw new RichTextValidationException(
                "Ảnh inline phải nằm trong khối news-image hoặc news-gallery-images.");
        }

        var src = GetAttributeValue(attributes, "src");
        var alt = GetAttributeValue(attributes, "alt");
        src = src is null ? null : WebUtility.HtmlDecode(src);
        alt = alt is null ? null : WebUtility.HtmlDecode(alt);
        if (src is null || !IsHttpsUrl(src) || alt is null)
        {
            throw new RichTextValidationException(
                "Ảnh inline phải có src HTTPS và thuộc tính alt.");
        }

        if (isSingleImage && parent!.FigureState!.ImageCount >= 1
            || isGalleryImage && parent!.GalleryImageCount >= 2)
        {
            throw new RichTextValidationException(
                "Số lượng ảnh trong khối ảnh bài viết không hợp lệ.");
        }

        result.Append(
            $"<img src=\"{WebUtility.HtmlEncode(src)}\" alt=\"{WebUtility.HtmlEncode(alt)}\">");
        if (isSingleImage)
        {
            parent!.FigureState!.ImageCount++;
        }
        else
        {
            parent!.GalleryImageCount++;
            var figure = openElements.LastOrDefault(element => element.FigureState is not null);
            if (figure?.FigureState is not null)
            {
                figure.FigureState.ImageCount++;
            }
        }
    }

    private static void ValidateNewsTextTagParent(
        string tagName,
        List<NewsOpenElement> openElements)
    {
        var parent = GetTopElement(openElements);
        if (parent?.IsGalleryImagesContainer == true
            || parent?.FigureState is not null && parent.TagName == "figure")
        {
            throw new RichTextValidationException(
                $"Thẻ {tagName} không được đặt trực tiếp trong khối ảnh.");
        }
    }

    private static NewsOpenElement? GetTopElement(List<NewsOpenElement> openElements)
    {
        return openElements.Count == 0 ? null : openElements[^1];
    }

    private static string SanitizeTag(string token, bool allowLinks)
    {
        return SanitizeTag(token, allowLinks, AllowedTags);
    }

    private static string SanitizeTag(
        string token,
        bool allowLinks,
        HashSet<string> allowedTags)
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
        if (!allowedTags.Contains(tagName) || (tagName == "a" && !allowLinks))
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

    private sealed class NewsOpenElement
    {
        public NewsOpenElement(
            string tagName,
            NewsFigureState? figureState = null,
            bool isGalleryImagesContainer = false)
        {
            TagName = tagName;
            FigureState = figureState;
            IsGalleryImagesContainer = isGalleryImagesContainer;
        }

        public string TagName { get; }

        public NewsFigureState? FigureState { get; }

        public bool IsGalleryImagesContainer { get; }

        public int GalleryImageCount { get; set; }
    }

    private sealed class NewsFigureState
    {
        public NewsFigureState(string className)
        {
            ClassName = className;
        }

        public string ClassName { get; }

        public int ImageCount { get; set; }

        public int CaptionCount { get; set; }

        public bool HasGalleryImagesContainer { get; set; }
    }
}

public sealed class RichTextValidationException : Exception
{
    public RichTextValidationException(string message)
        : base(message)
    {
    }
}
