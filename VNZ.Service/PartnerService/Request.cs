using Microsoft.AspNetCore.Http;

namespace VNZ.Service.PartnerService;

public class Request
{
    public class CreatePartnerRequest
    {
        public string Name { get; set; } = string.Empty;
        public IFormFile? Logo { get; set; }
        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Description { get; set; }
    }

    public class UpdatePartnerRequest
    {
        public string Name { get; set; } = string.Empty;
        public IFormFile? Logo { get; set; }
        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Description { get; set; }
        public bool? IsPublished { get; set; }
    }

    public class GetPartnerListRequest
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ReorderPartnersRequest
    {
        public List<Guid>? OrderedPartnerIds { get; set; }
    }
}
