using System.Text.Json.Serialization;
using VNZ.Repository.Entity.Enum;

namespace VNZ.Repository.Entity.Json;

public class ProductImages
{
    [JsonPropertyName("items")]
    public List<ProductImage> Items { get; set; } = new();
}

public class ProductImage
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductImageType Type { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
