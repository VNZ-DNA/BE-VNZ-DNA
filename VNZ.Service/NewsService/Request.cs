namespace VNZ.Service.NewsService;

public class Request
{
    public class CreateNewsRequest
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public List<Guid>? CategoryIds { get; set; } = new();
        public string? Status { get; set; }
    }

    public class GetNewsListRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public Guid? CategoryId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
