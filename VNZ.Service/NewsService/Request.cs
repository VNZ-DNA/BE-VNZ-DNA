namespace VNZ.Service.NewsService;

public class Request
{
    public class GetNewsListRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public Guid? CategoryId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
