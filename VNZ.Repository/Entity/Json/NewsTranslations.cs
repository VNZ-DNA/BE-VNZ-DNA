using System.Text.Json.Serialization;

namespace VNZ.Repository.Entity.Json;

public sealed class NewsTranslations
{
    [JsonPropertyName("en")]
    public NewsEnglishTranslation? En { get; set; }
}

public sealed class NewsEnglishTranslation
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
