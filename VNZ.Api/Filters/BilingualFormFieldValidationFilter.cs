using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Filters;
using VNZ.Service.Exceptions;

namespace VNZ.Api.Filters;

/// <summary>
/// ASP.NET model binding ignores unknown multipart keys by default. The
/// bilingual contracts are replacement contracts, so silently ignoring a typo
/// can publish an incomplete revision. Validate the form key set before the
/// controller action executes and report the exact key path.
/// </summary>
public sealed class BilingualFormFieldValidationFilter : IAsyncActionFilter
{
    private static readonly Regex NewsCategoryIdKey = new(
        "^categoryIds\\[\\d+\\]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ProductContentKey = new(
        "^(content|translations\\.en\\.content)(\\.blocks(\\[\\d+\\])?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ProductBlockKey = new(
        "^(content|translations\\.en\\.content)\\.blocks\\[\\d+\\](\\.(id|type|order|text|items))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ProductItemKey = new(
        "^(content|translations\\.en\\.content)\\.blocks\\[\\d+\\]\\.items\\[\\d+\\](\\.(id|title))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (IsBilingualMultipartEndpoint(context.HttpContext))
        {
            string[] unknownKeys;

            try
            {
                unknownKeys = context.HttpContext.Request.Form.Keys
                    .Where(key => !IsAllowedKey(context.HttpContext, key))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
            {
                throw new BilingualSchemaException(
                    "Multipart request cannot be parsed.",
                    new[] { "$" },
                    exception);
            }

            if (unknownKeys.Length > 0)
            {
                throw new BilingualSchemaException(
                    "Request chứa field multipart không nằm trong schema song ngữ.",
                    unknownKeys);
            }
        }

        await next();
    }

    private static bool IsBilingualMultipartEndpoint(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return false;
        }

        var path = context.Request.Path.Value;
        if (path is null)
        {
            return false;
        }

        var isNewsWrite = path.Equals("/api/v1/admin/news", StringComparison.OrdinalIgnoreCase) ||
            (path.StartsWith("/api/v1/admin/news/", StringComparison.OrdinalIgnoreCase) &&
             HttpMethods.IsPut(context.Request.Method));
        var isProductWrite = path.Equals("/api/v1/admin/products", StringComparison.OrdinalIgnoreCase) ||
            (path.StartsWith("/api/v1/admin/products/", StringComparison.OrdinalIgnoreCase) &&
             HttpMethods.IsPut(context.Request.Method));

        return (isNewsWrite || isProductWrite) &&
            (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method));
    }

    private static bool IsAllowedKey(HttpContext context, string key)
    {
        var normalized = key.Trim();

        if (normalized.Equals("translations", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("translations.en", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.Request.Path.Value!.Contains("/news", StringComparison.OrdinalIgnoreCase))
        {
            return IsAllowedNewsKey(context.Request.Method, normalized);
        }

        return IsAllowedProductKey(context.Request.Method, normalized);
    }

    private static bool IsAllowedNewsKey(string method, string key)
    {
        var common = key.Equals("title", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("summary", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("content", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("image", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("status", StringComparison.OrdinalIgnoreCase) ||
            NewsCategoryIdKey.IsMatch(key) ||
            key.Equals("categoryIds", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("translations.en.title", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("translations.en.summary", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("translations.en.content", StringComparison.OrdinalIgnoreCase);

        if (common)
        {
            return true;
        }

        return HttpMethods.IsPut(method) &&
            (key.Equals("action", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("expectedUpdatedAt", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAllowedProductKey(string method, string key)
    {
        var common = key.Equals("name", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("logo", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("wordmark", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("productUrl", StringComparison.OrdinalIgnoreCase) ||
            ProductContentKey.IsMatch(key) ||
            ProductBlockKey.IsMatch(key) ||
            ProductItemKey.IsMatch(key) ||
            key.Equals("content.blocks", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("translations.en.content.blocks", StringComparison.OrdinalIgnoreCase);

        if (common)
        {
            return true;
        }

        return HttpMethods.IsPut(method) &&
            (key.Equals("logoAction", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("wordmarkAction", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("status", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("isPublished", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("expectedUpdatedAt", StringComparison.OrdinalIgnoreCase));
    }
}
