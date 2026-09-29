namespace VNZ.Service.PartnerService;

public class Response
{
    public class PublicPartnerListResponse
    {
        public List<PublicPartnerListItemResponse> Items { get; set; } = new();
        public int Total { get; set; }
    }

    public class PublicPartnerListItemResponse
    {
        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
    }

    public class PagedPartnerListResponse
    {
        public List<PartnerListItemResponse> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    public class PartnerListItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Description { get; set; }
        public bool IsPublished { get; set; }
        public int? DisplayOrder { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

}
