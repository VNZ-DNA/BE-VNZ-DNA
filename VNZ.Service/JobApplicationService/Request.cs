namespace VNZ.Service.JobApplicationService;

public static class Request
{
    public class GetJobApplicationListRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ReviewJobApplicationRequest
    {
        public string? Decision { get; set; }
    }
}
