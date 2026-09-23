namespace VNZ.Service.DashboardService;

public class Response
{
    public class DashboardResponse
    {
        public int TotalNewsCount { get; init; }
        public required IReadOnlyDictionary<string, int> NewsByStatus { get; init; }
        public int OpenJobsCount { get; init; }
        public int PendingApplicationsCount { get; init; }
        public int UnreadContactsCount { get; init; }
        public required IReadOnlyList<PendingTaskResponse> PendingTasks { get; init; }
        public required IReadOnlyList<RecentPostResponse> RecentPosts { get; init; }
    }

    public class PendingTaskResponse
    {
        public required string Type { get; init; }
        public int Count { get; init; }
        public required string Label { get; init; }
    }

    public class RecentPostResponse
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public required string AuthorName { get; init; }
        public required string Status { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
