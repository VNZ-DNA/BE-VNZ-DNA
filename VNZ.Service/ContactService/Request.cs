namespace VNZ.Service.ContactService;

public class Request
{
    public class GetContactListRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SendContactReplyRequest
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
