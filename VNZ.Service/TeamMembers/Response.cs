using VNZ.Repository.Entity.Enum;

namespace VNZ.Service.TeamMembers;

public static class Response
{
    public class TeamMemberResponse
    {
        public Guid Id { get; init; }
        public Guid? RoleId { get; init; }
        public Guid? CreatedBy { get; init; }
        public required string FullName { get; init; }
        public required string Email { get; init; }
        public string? Position { get; init; }
        public JobLevel? JobLevel { get; init; }
        public string? AvatarUrl { get; init; }
        public string? AnimationUrl { get; init; }
        public string? AudioUrl { get; init; }
        public string? Hometown { get; init; }
        public string? Hobbies { get; init; }
        public string? PersonalQuote { get; init; }
        public DateTimeOffset? JoinedDate { get; init; }
        public EmploymentStatus EmploymentStatus { get; init; }
        public bool IsActive { get; init; }
        public bool IsPublished { get; init; }
        public int? DisplayOrder { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
