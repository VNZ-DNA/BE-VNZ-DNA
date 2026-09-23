using VNZ.Repository.Entity.Enum;

namespace VNZ.Repository.Entity.Json;

public class ProductContent
{
    public List<ContentBlock> Blocks { get; set; } = new();
}

public class ContentBlock
{
    public Guid Id { get; set; }
    public ContentBlockType Type { get; set; }
    public int Order { get; set; }
    public string? Text { get; set; }
    public List<FeatureItem>? Items { get; set; }
}

public class FeatureItem
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
}
