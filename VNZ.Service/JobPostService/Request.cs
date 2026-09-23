namespace VNZ.Service.JobPostService;

public class Request
{
    public class GetJobPostListRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
