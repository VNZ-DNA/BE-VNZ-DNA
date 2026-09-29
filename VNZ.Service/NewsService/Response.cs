namespace VNZ.Service.NewsService;

public class Response
{
    public class UploadContentImageResponse
    {
        public required string Url { get; set; }
        public required string PublicId { get; set; }
        public required string Format { get; set; }
        public long Bytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }

    public class PagedNewsListResponse
    {
        public required List<NewsListItemResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class PagedPublicNewsListResponse
    {
        public required List<PublicNewsListItemResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class PublicNewsListItemResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Summary { get; set; }
        public DateTimeOffset PublishAt { get; set; }
        public int ReadingTimeMinutes { get; set; }
        public required List<NewsCategoryResponse> Categories { get; set; }
    }

    public class PublicNewsDetailResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTimeOffset PublishAt { get; set; }
        public int ReadingTimeMinutes { get; set; }
        public required List<NewsCategoryResponse> Categories { get; set; }
    }

    public class NewsListItemResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? ImageUrl { get; set; }
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
        public string? ImageUrl { get; set; }
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
        public string? ImageUrl { get; set; }
        public required string AuthorName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateOnly? UpdatedAt { get; set; }
        public DateTimeOffset? PublishAt { get; set; }
        public required string Status { get; set; }
        public required List<NewsCategoryResponse> Categories { get; set; }
    }

    public class UpdateNewsResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
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
