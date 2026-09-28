using VNZ.Repository.Entity.Enum;
using System.Text.Json.Serialization;

namespace VNZ.Repository.Entity.Json;

public class ProductContent
{
    [JsonPropertyName("blocks")]
    public List<ContentBlock> Blocks { get; set; } = new();
}

public class ContentBlock
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentBlockType Type { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("items")]
    public List<FeatureItem>? Items { get; set; }
}

public class FeatureItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
