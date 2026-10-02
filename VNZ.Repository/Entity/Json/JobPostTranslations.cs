using System.Text.Json.Serialization;

namespace VNZ.Repository.Entity.Json;

public sealed class JobPostTranslations
{
    [JsonPropertyName("en")]
    public JobPostEnglishTranslation? En { get; set; }
}

public sealed class JobPostEnglishTranslation
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("requirements")]
    public string? Requirements { get; set; }
}
