using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using RichTextService = VNZ.Service.Utils.RichTextService;

namespace VNZ.Service.Localization;

public sealed record BilingualIntegrityIssue(
    string Entity,
    Guid Id,
    string FieldPath,
    string Reason);

public sealed class BilingualIntegrityCheckResult
{
    public DateTimeOffset CheckedAtUtc { get; init; }
    public IReadOnlyList<BilingualIntegrityIssue> Issues { get; init; } = Array.Empty<BilingualIntegrityIssue>();
    public string? InfrastructureError { get; init; }
    public bool IsHealthy => InfrastructureError is null && Issues.Count == 0;
    public int ExitCode => InfrastructureError is not null ? 2 : IsHealthy ? 0 : 1;
}

public interface IBilingualIntegrityChecker
{
    Task<BilingualIntegrityCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only release gate for all currently public records. It reads JSONB as
/// raw text so one malformed legacy document does not abort the scan before
/// the remaining records have been reported.
/// </summary>
public sealed class BilingualIntegrityChecker : IBilingualIntegrityChecker
{
    private static readonly JsonSerializerOptions StrictJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly HashSet<string> NewsEnglishProperties = new(
        new[] { "title", "summary", "content" },
        StringComparer.Ordinal);

    private static readonly HashSet<string> JobPostEnglishProperties = new(
        new[] { "title", "shortDescription", "description", "requirements" },
        StringComparer.Ordinal);

    private static readonly RegexLike StableCodePattern = new("^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*$");

    private readonly AppDbContext _dbContext;
    private readonly RichTextService.IService _richTextService;

    public BilingualIntegrityChecker(
        AppDbContext dbContext,
        RichTextService.IService richTextService)
    {
        _dbContext = dbContext;
        _richTextService = richTextService;
    }

    public async Task<BilingualIntegrityCheckResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = DateTimeOffset.UtcNow;
        var issues = new List<BilingualIntegrityIssue>();

        try
        {
            await CheckLookupCodesAsync(issues, cancellationToken);

            var news = await ReadRowsAsync<NewsRow>(
                """
                SELECT "Id", "Title", "Summary", "Content", "Status", "Published", "PublishAt", "Translations"::text
                FROM "News_Article"
                WHERE "Status" = 'Published' OR "Published" = TRUE
                """,
                reader => new NewsRow(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    GetNullableString(reader, 2),
                    GetNullableString(reader, 3),
                    GetNullableString(reader, 4),
                    !reader.IsDBNull(5) && reader.GetBoolean(5),
                    GetNullableDateTimeOffset(reader, 6),
                    GetNullableString(reader, 7)),
                cancellationToken);

            foreach (var row in news)
            {
                CheckNews(row, issues);
            }

            var jobPosts = await ReadRowsAsync<JobPostRow>(
                """
                SELECT "Id", "Title", "ShortDescription", "Description", "Requirements", "Status", "Translations"::text
                FROM "Job_Post"
                WHERE "Status" = 'Open'
                """,
                reader => new JobPostRow(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    GetNullableString(reader, 2),
                    GetNullableString(reader, 3),
                    GetNullableString(reader, 4),
                    GetNullableString(reader, 5),
                    GetNullableString(reader, 6)),
                cancellationToken);

            foreach (var row in jobPosts)
            {
                CheckJobPost(row, issues);
            }

            var products = await ReadRowsAsync<ProductRow>(
                """
                SELECT "Id", "LogoUrl", "WordmarkUrl", "Content"::text, "Translations"::text
                FROM "Product"
                WHERE "IsPublished" = TRUE
                """,
                reader => new ProductRow(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    GetNullableString(reader, 2),
                    GetNullableString(reader, 3),
                    GetNullableString(reader, 4)),
                cancellationToken);

            foreach (var row in products)
            {
                CheckProduct(row, issues);
            }

            return new BilingualIntegrityCheckResult
            {
                CheckedAtUtc = checkedAtUtc,
                Issues = issues
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new BilingualIntegrityCheckResult
            {
                CheckedAtUtc = checkedAtUtc,
                Issues = issues,
                InfrastructureError = $"{exception.GetType().Name}: {exception.Message}"
            };
        }
    }

    private async Task CheckLookupCodesAsync(
        ICollection<BilingualIntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var departments = await _dbContext.Departments
            .AsNoTracking()
            .Select(item => new { item.Id, item.Code })
            .ToListAsync(cancellationToken);

        foreach (var group in departments.GroupBy(item => item.Code ?? string.Empty, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1)
            {
                foreach (var item in group)
                {
                    AddIssue(issues, "Department", item.Id, "code", "Code is empty or duplicated.");
                }
            }
            else if (!StableCodePattern.IsMatch(group.Key) || group.Key.Length > 100)
            {
                foreach (var item in group)
                {
                    AddIssue(issues, "Department", item.Id, "code", "Code is not a stable uppercase key.");
                }
            }
        }

        var categories = await _dbContext.NewsCategories
            .AsNoTracking()
            .Select(item => new { item.Id, item.Code })
            .ToListAsync(cancellationToken);

        foreach (var group in categories.GroupBy(item => item.Code ?? string.Empty, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1)
            {
                foreach (var item in group)
                {
                    AddIssue(issues, "NewsCategory", item.Id, "code", "Code is empty or duplicated.");
                }
            }
            else if (!StableCodePattern.IsMatch(group.Key) || group.Key.Length > 100)
            {
                foreach (var item in group)
                {
                    AddIssue(issues, "NewsCategory", item.Id, "code", "Code is not a stable uppercase key.");
                }
            }
        }
    }

    private void CheckNews(NewsRow row, ICollection<BilingualIntegrityIssue> issues)
    {
        Required(row, issues, "title", row.Title);
        Required(row, issues, "summary", row.Summary);
        Required(row, issues, "content", row.Content);

        if (!row.Published)
        {
            AddIssue(issues, "News", row.Id, "published", "Status=Published must have Published=true.");
        }

        if (!row.PublishAt.HasValue)
        {
            AddIssue(issues, "News", row.Id, "publishAt", "Status=Published must have PublishAt.");
        }

        CheckNewsRichText(row.Id, "summary", row.Summary, issues, isContent: false);
        CheckNewsRichText(row.Id, "content", row.Content, issues, isContent: true);
        CheckNewsTranslation(row, issues);
    }

    private void CheckNewsTranslation(NewsRow row, ICollection<BilingualIntegrityIssue> issues)
    {
        if (!TryParseObject(row.Translations, "News", row.Id, "translations", issues, out var root))
        {
            return;
        }

        CheckUnknownProperties(root, "News", row.Id, "translations", new[] { "en" }, issues);

        if (!root.TryGetProperty("en", out var english) || english.ValueKind != JsonValueKind.Object)
        {
            AddIssue(issues, "News", row.Id, "translations.en", "English translation object is missing.");
            return;
        }

        CheckUnknownProperties(english, "News", row.Id, "translations.en", NewsEnglishProperties, issues);
        CheckRequiredJsonString(english, "title", "News", row.Id, issues);
        CheckRequiredJsonString(english, "summary", "News", row.Id, issues);
        CheckRequiredJsonString(english, "content", "News", row.Id, issues);

        CheckNewsRichText(row.Id, "translations.en.summary", GetJsonString(english, "summary"), issues, false);
        CheckNewsRichText(row.Id, "translations.en.content", GetJsonString(english, "content"), issues, true);
    }

    private void CheckJobPost(JobPostRow row, ICollection<BilingualIntegrityIssue> issues)
    {
        Required(row, issues, "title", row.Title);
        Required(row, issues, "shortDescription", row.ShortDescription);
        Required(row, issues, "description", row.Description);
        Required(row, issues, "requirements", row.Requirements);

        if (!TryParseObject(row.Translations, "JobPost", row.Id, "translations", issues, out var root))
        {
            return;
        }

        CheckUnknownProperties(root, "JobPost", row.Id, "translations", new[] { "en" }, issues);

        if (!root.TryGetProperty("en", out var english) || english.ValueKind != JsonValueKind.Object)
        {
            AddIssue(issues, "JobPost", row.Id, "translations.en", "English translation object is missing.");
            return;
        }

        CheckUnknownProperties(english, "JobPost", row.Id, "translations.en", JobPostEnglishProperties, issues);
        foreach (var property in JobPostEnglishProperties)
        {
            CheckRequiredJsonString(english, property, "JobPost", row.Id, issues);
        }
    }

    private void CheckProduct(ProductRow row, ICollection<BilingualIntegrityIssue> issues)
    {
        Required(row, issues, "logoUrl", row.LogoUrl);
        Required(row, issues, "wordmarkUrl", row.WordmarkUrl);

        var vietnamese = ParseProductContent(row.Content, "Product", row.Id, "content", issues);
        ProductContent? english = null;

        if (vietnamese is null &&
            (string.IsNullOrWhiteSpace(row.Translations) ||
             string.Equals(row.Translations.Trim(), "null", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (!TryParseObject(row.Translations, "Product", row.Id, "translations", issues, out var root))
        {
            if (vietnamese is not null)
            {
                AddIssue(issues, "Product", row.Id, "translations.en.content", "English content is missing.");
            }

            return;
        }

        CheckUnknownProperties(root, "Product", row.Id, "translations", new[] { "en" }, issues);

        if (!root.TryGetProperty("en", out var en) || en.ValueKind != JsonValueKind.Object)
        {
            if (vietnamese is not null)
            {
                AddIssue(issues, "Product", row.Id, "translations.en", "English translation object is missing.");
            }

            return;
        }

        CheckUnknownProperties(en, "Product", row.Id, "translations.en", new[] { "content" }, issues);
        if (en.TryGetProperty("content", out var englishElement) &&
            englishElement.ValueKind != JsonValueKind.Null)
        {
            english = ParseProductContent(
                englishElement.GetRawText(),
                "Product",
                row.Id,
                "translations.en.content",
                issues);
        }

        if (vietnamese is null && english is null)
        {
            return;
        }

        if (vietnamese is null || english is null)
        {
            AddIssue(
                issues,
                "Product",
                row.Id,
                vietnamese is null ? "content" : "translations.en.content",
                "Published Product must contain both VI and EN Content when either side exists.");
            return;
        }

        if (!HasSameProductContentShape(vietnamese, english))
        {
            AddIssue(issues, "Product", row.Id, "translations.en.content", "VI/EN Product Content shape differs.");
        }

        CheckCompleteProductContent(row.Id, "content", vietnamese, issues);
        CheckCompleteProductContent(row.Id, "translations.en.content", english, issues);
    }

    private ProductContent? ParseProductContent(
        string? raw,
        string entity,
        Guid id,
        string path,
        ICollection<BilingualIntegrityIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw.Trim(), "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                AddIssue(issues, entity, id, path, "Product Content must be an object.");
                return null;
            }

            CheckUnknownProperties(
                document.RootElement,
                entity,
                id,
                path,
                new[] { "blocks" },
                issues);

            if (!document.RootElement.TryGetProperty("blocks", out var blocks) ||
                blocks.ValueKind != JsonValueKind.Array)
            {
                AddIssue(issues, entity, id, $"{path}.blocks", "Product Content blocks must be an array.");
                return null;
            }

            for (var blockIndex = 0; blockIndex < blocks.GetArrayLength(); blockIndex++)
            {
                var block = blocks[blockIndex];
                if (block.ValueKind != JsonValueKind.Object)
                {
                    AddIssue(issues, entity, id, $"{path}.blocks[{blockIndex}]", "Block must be an object.");
                    continue;
                }

                CheckUnknownProperties(
                    block,
                    entity,
                    id,
                    $"{path}.blocks[{blockIndex}]",
                    new[] { "id", "type", "order", "text", "items" },
                    issues);

                if (block.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    for (var itemIndex = 0; itemIndex < items.GetArrayLength(); itemIndex++)
                    {
                        var item = items[itemIndex];
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            CheckUnknownProperties(
                                item,
                                entity,
                                id,
                                $"{path}.blocks[{blockIndex}].items[{itemIndex}]",
                                new[] { "id", "title" },
                                issues);
                        }
                    }
                }
            }

            var content = JsonSerializer.Deserialize<ProductContent>(raw, StrictJsonOptions);
            ValidateProductContentShape(content, entity, id, path, issues);
            return content;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            AddIssue(issues, entity, id, path, "Product Content JSON does not match the typed schema.");
            return null;
        }
    }

    private static void ValidateProductContentShape(
        ProductContent? content,
        string entity,
        Guid id,
        string path,
        ICollection<BilingualIntegrityIssue> issues)
    {
        if (content is null)
        {
            return;
        }

        var ids = new HashSet<Guid>();
        var orders = new HashSet<int>();

        foreach (var (block, blockIndex) in content.Blocks.Select((value, index) => (value, index)))
        {
            var blockPath = $"{path}.blocks[{blockIndex}]";
            if (block.Id == Guid.Empty || !ids.Add(block.Id))
            {
                AddIssue(issues, entity, id, $"{blockPath}.id", "Block id must be non-empty and unique.");
            }

            if (!Enum.IsDefined(block.Type))
            {
                AddIssue(issues, entity, id, $"{blockPath}.type", "Block type is not supported.");
            }

            if (!orders.Add(block.Order))
            {
                AddIssue(issues, entity, id, $"{blockPath}.order", "Block order must be unique.");
            }

            if (block.Type == ContentBlockType.Feature)
            {
                if (block.Text is not null || block.Items is null)
                {
                    AddIssue(issues, entity, id, blockPath, "Feature block requires items and no text.");
                }

                var itemIds = new HashSet<Guid>();
                foreach (var (item, itemIndex) in (block.Items ?? new List<FeatureItem>()).Select((value, index) => (value, index)))
                {
                    if (item.Id == Guid.Empty || !itemIds.Add(item.Id))
                    {
                        AddIssue(issues, entity, id, $"{blockPath}.items[{itemIndex}].id", "Feature item id must be non-empty and unique.");
                    }
                }
            }
            else if (block.Items is not null)
            {
                AddIssue(issues, entity, id, $"{blockPath}.items", "Only Feature blocks may contain items.");
            }
        }

        for (var order = 1; order <= content.Blocks.Count; order++)
        {
            if (!orders.Contains(order))
            {
                AddIssue(issues, entity, id, $"{path}.blocks.order", "Block order must be the contiguous set 1..N.");
                break;
            }
        }
    }

    private static bool HasSameProductContentShape(ProductContent vietnamese, ProductContent english)
    {
        if (vietnamese.Blocks.Count != english.Blocks.Count)
        {
            return false;
        }

        for (var index = 0; index < vietnamese.Blocks.Count; index++)
        {
            var viBlock = vietnamese.Blocks[index];
            var enBlock = english.Blocks[index];

            if (viBlock.Id != enBlock.Id || viBlock.Type != enBlock.Type || viBlock.Order != enBlock.Order)
            {
                return false;
            }

            if (viBlock.Type != ContentBlockType.Feature)
            {
                continue;
            }

            if (viBlock.Items is null || enBlock.Items is null || viBlock.Items.Count != enBlock.Items.Count)
            {
                return false;
            }

            for (var itemIndex = 0; itemIndex < viBlock.Items.Count; itemIndex++)
            {
                if (viBlock.Items[itemIndex].Id != enBlock.Items[itemIndex].Id)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void CheckCompleteProductContent(
        Guid id,
        string path,
        ProductContent content,
        ICollection<BilingualIntegrityIssue> issues)
    {
        foreach (var (block, blockIndex) in content.Blocks.Select((value, index) => (value, index)))
        {
            if (block.Type == ContentBlockType.Feature)
            {
                foreach (var (item, itemIndex) in (block.Items ?? new List<FeatureItem>()).Select((value, index) => (value, index)))
                {
                    if (string.IsNullOrWhiteSpace(item.Title))
                    {
                        AddIssue(issues, "Product", id, $"{path}.blocks[{blockIndex}].items[{itemIndex}].title", "Published text is missing.");
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(block.Text))
            {
                AddIssue(issues, "Product", id, $"{path}.blocks[{blockIndex}].text", "Published text is missing.");
            }
        }
    }

    private void CheckNewsRichText(
        Guid id,
        string path,
        string? value,
        ICollection<BilingualIntegrityIssue> issues,
        bool isContent)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            var sanitized = isContent
                ? _richTextService.SanitizeNewsContent(value)
                : _richTextService.SanitizeNewsSummary(value);

            if (isContent && CountNonWhitespace(_richTextService.ToPlainText(sanitized ?? string.Empty)) < 300)
            {
                AddIssue(issues, "News", id, path, "Published content is shorter than 300 non-whitespace characters.");
            }
        }
        catch (Exception)
        {
            AddIssue(issues, "News", id, path, "Rich content does not pass the published sanitizer rules.");
        }
    }

    private static int CountNonWhitespace(string value)
    {
        return value.Count(character => !char.IsWhiteSpace(character));
    }

    private static void CheckRequiredJsonString(
        JsonElement objectElement,
        string property,
        string entity,
        Guid id,
        ICollection<BilingualIntegrityIssue> issues)
    {
        if (!objectElement.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            AddIssue(issues, entity, id, $"translations.en.{property}", "Published English field is missing.");
        }
    }

    private static string? GetJsonString(JsonElement objectElement, string property)
    {
        return objectElement.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool TryParseObject(
        string? raw,
        string entity,
        Guid id,
        string path,
        ICollection<BilingualIntegrityIssue> issues,
        out JsonElement root)
    {
        root = default;
        if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw.Trim(), "null", StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(issues, entity, id, path, "Translation JSON is missing.");
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                AddIssue(issues, entity, id, path, "Translation JSON must be an object.");
                return false;
            }

            // Clone detaches the element from the disposable JsonDocument.
            root = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            AddIssue(issues, entity, id, path, "Translation JSON is malformed.");
            return false;
        }
    }

    private static void CheckUnknownProperties(
        JsonElement objectElement,
        string entity,
        Guid id,
        string path,
        IEnumerable<string> allowedProperties,
        ICollection<BilingualIntegrityIssue> issues)
    {
        var allowed = allowedProperties.ToHashSet(StringComparer.Ordinal);
        foreach (var property in objectElement.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                AddIssue(issues, entity, id, $"{path}.{property.Name}", "Unknown JSON property.");
            }
        }
    }

    private static void Required<T>(
        T row,
        ICollection<BilingualIntegrityIssue> issues,
        string path,
        string? value)
        where T : IIntegrityRow
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddIssue(issues, row.EntityName, row.Id, path, "Published source field is missing.");
        }
    }

    private static void AddIssue(
        ICollection<BilingualIntegrityIssue> issues,
        string entity,
        Guid id,
        string fieldPath,
        string reason)
    {
        issues.Add(new BilingualIntegrityIssue(entity, id, fieldPath, reason));
    }

    private async Task<List<T>> ReadRowsAsync<T>(
        string sql,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<T>();

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(map(reader));
        }

        if (openedHere)
        {
            await connection.CloseAsync();
        }

        return rows;
    }

    private static string? GetNullableString(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    private interface IIntegrityRow
    {
        Guid Id { get; }
        string EntityName { get; }
    }

    private sealed record NewsRow(
        Guid Id,
        string? Title,
        string? Summary,
        string? Content,
        string? Status,
        bool Published,
        DateTimeOffset? PublishAt,
        string? Translations) : IIntegrityRow
    {
        public string EntityName => "News";
    }

    private sealed record JobPostRow(
        Guid Id,
        string? Title,
        string? ShortDescription,
        string? Description,
        string? Requirements,
        string? Status,
        string? Translations) : IIntegrityRow
    {
        public string EntityName => "JobPost";
    }

    private sealed record ProductRow(
        Guid Id,
        string? LogoUrl,
        string? WordmarkUrl,
        string? Content,
        string? Translations) : IIntegrityRow
    {
        public string EntityName => "Product";
    }

    // A tiny allocation-free-enough wrapper keeps this source independent from
    // a regex package while retaining a readable stable-code rule.
    private sealed class RegexLike
    {
        private readonly System.Text.RegularExpressions.Regex _regex;

        public RegexLike(string pattern)
        {
            _regex = new System.Text.RegularExpressions.Regex(
                pattern,
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        }

        public bool IsMatch(string value) => _regex.IsMatch(value);
    }
}
