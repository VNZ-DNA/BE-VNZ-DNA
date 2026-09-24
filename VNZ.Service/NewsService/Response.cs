namespace VNZ.Service.NewsService;

public class Response
{
    public class PagedNewsListResponse
    {
        public required List<NewsListItemResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class NewsListItemResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string AuthorName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PublishAt { get; set; }
        public required string Status { get; set; }
        public required List<NewsCategoryResponse> Categories { get; set; }
    }

    public class NewsDetailResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public required string AuthorName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateOnly? UpdatedAt { get; set; }
        public DateTimeOffset? PublishAt { get; set; }
        public required string Status { get; set; }
        public required List<NewsCategoryResponse> Categories { get; set; }
        public required List<string> Actions { get; set; }
    }

    public class CreateNewsResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public required string AuthorName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateOnly? UpdatedAt { get; set; }
        public DateTimeOffset? PublishAt { get; set; }
        public required string Status { get; set; }
        public required List<NewsCategoryResponse> Categories { get; set; }
    }

    public class NewsCategoryResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
    }
}
