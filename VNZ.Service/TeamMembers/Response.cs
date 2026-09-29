namespace VNZ.Service.TeamMembers;

public static class Response
{
    public class FeaturedTeamMembersResponse
    {
        public int Total { get; set; }
        public required List<FeaturedTeamMemberResponse> Items { get; set; }
    }

    public class FeaturedTeamMemberResponse
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Hometown { get; set; }
        public string? BackgroundUrl { get; set; }
    }

    public class PublicTeamMemberResponse
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public string? Position { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class PublicTeamMemberDetailResponse
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Hometown { get; set; }
        public string? BackgroundUrl { get; set; }
        public string? Hobbies { get; set; }
        public DateTimeOffset? JoinedDate { get; set; }
        public string? PersonalQuote { get; set; }
        public string? AnimationUrl { get; set; }
        public string? AudioUrl { get; set; }
    }

    public class PagedTeamMemberListResponse
    {
        public required List<TeamMemberResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    public class OrderableTeamMemberResponse
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public string? Position { get; set; }
        public string? AvatarUrl { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class TeamMemberResponse
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AnimationUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? Hometown { get; set; }
        public string? BackgroundUrl { get; set; }
        public string? Hobbies { get; set; }
        public string? PersonalQuote { get; set; }
        public DateTimeOffset? JoinedDate { get; set; }
        public required string EmploymentStatus { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public int? DisplayOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
