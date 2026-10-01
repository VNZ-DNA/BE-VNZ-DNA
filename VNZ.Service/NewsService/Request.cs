using Microsoft.AspNetCore.Http;

namespace VNZ.Service.NewsService;

public class  Request
{
    public class UploadContentImageRequest
    {
        public IFormFile? File { get; set; }
    }

    public class CreateNewsRequest
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public IFormFile? Image { get; set; }
        public List<Guid>? CategoryIds { get; set; } = new();
        public string? Status { get; set; }
    }

    public class UpdateNewsRequest
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public IFormFile? Image { get; set; }
        public string? Action { get; set; }
        public List<Guid>? CategoryIds { get; set; } = new();
        public string? Status { get; set; }
    }

    public class GetNewsListRequest
    {
        public string? Search { get; set; }
        public List<string>? Status { get; set; }
        public List<string>? CategoryId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetPublicNewsListRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 5;
    }
}
