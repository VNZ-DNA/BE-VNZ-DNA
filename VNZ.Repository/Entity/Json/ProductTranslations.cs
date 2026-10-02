using System.Text.Json.Serialization;

namespace VNZ.Repository.Entity.Json;

public sealed class ProductTranslations
{
    [JsonPropertyName("en")]
    public ProductEnglishTranslation? En { get; set; }
}

public sealed class ProductEnglishTranslation
{
    [JsonPropertyName("content")]
    public ProductContent? Content { get; set; }
}
